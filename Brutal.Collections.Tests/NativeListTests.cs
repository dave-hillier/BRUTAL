using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Brutal.Collections.Tests;

/// <summary>
/// Tests for the untyped, byte-addressed <see cref="NativeList"/>.
/// </summary>
public class UntypedNativeListTests
{
    [StructLayout(LayoutKind.Sequential)]
    private struct SixteenBytes
    {
        public long A;
        public long B;
    }

    /// <summary>
    /// Turns a failed Debug.Assert into an exception instead of Environment.FailFast,
    /// so a broken invariant inside the library fails the test rather than killing the test host.
    /// </summary>
    private sealed class ThrowingTraceListener : TraceListener
    {
        public override void Write(string? message) { }

        public override void WriteLine(string? message) { }

        public override void Fail(string? message, string? detailMessage) =>
            throw new InvalidOperationException($"Debug.Assert failed: {message} {detailMessage}");
    }

    private static readonly Lock ListenerLock = new();

    public UntypedNativeListTests()
    {
        lock (ListenerLock)
        {
            if (Trace.Listeners.OfType<ThrowingTraceListener>().Any())
                return;
            Trace.Listeners.Remove("Default");
            Trace.Listeners.Add(new ThrowingTraceListener());
        }
    }

    [Fact]
    public void AddingAStructLargerThanTheCurrentContents_GrowsCapacityToAtLeastTheNewSize()
    {
        using var list = new NativeList();
        list.Add((byte)1); // Size 1, Capacity 4

        list.Add(new SixteenBytes { A = 2, B = 3 });

        Assert.Equal(17, (int)list.Size);
        Assert.True(
            list.Capacity >= list.Size,
            $"Capacity {(int)list.Capacity} is smaller than Size {(int)list.Size}"
        );
    }
}
