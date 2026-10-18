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

    [Fact]
    public void InsertingAByteWithSpareCapacity_IncreasesSizeAndShiftsLaterBytes()
    {
        using var list = new NativeList(ByteSize.Of(16));
        list.Add((byte)1);
        list.Add((byte)2);
        list.Add((byte)3);

        list.Insert(ByteSize.Of(1), (byte)9);

        Assert.Equal(4, (int)list.Size);
        Assert.Equal(new byte[] { 1, 9, 2, 3 }, list.AsSpan<byte>().ToArray());
    }

    [Fact]
    public void InsertingAByteIntoAFullList_GrowsCapacityToAtLeastTheNewSize()
    {
        using var list = new NativeList([1, 2, 3, 4]); // Size 4, Capacity 4

        list.Insert(ByteSize.Of(0), (byte)9);

        Assert.True(
            (int)list.Capacity >= 5,
            $"Capacity {(int)list.Capacity} was not updated after a growing insert to 5 bytes"
        );
    }

    [Fact]
    public void InsertingATypedValueIntoAFullList_GrowsCapacityToAtLeastTheNewSize()
    {
        using var list = new NativeList([1, 2, 3, 4]); // Size 4, Capacity 4

        list.Insert(ByteSize.Of(0), 0x0A0B0C0D);

        Assert.Equal(8, (int)list.Size);
        Assert.True(
            list.Capacity >= list.Size,
            $"Capacity {(int)list.Capacity} is smaller than Size {(int)list.Size} after a growing insert"
        );
    }

    [Fact]
    public void RepeatedTypedInserts_ReallocateGeometricallyNotOnEveryInsert()
    {
        const int inserts = 64;
        using var list = new NativeList();
        var reallocations = 0;
        var previous = list.Ptr;

        for (var i = 0; i < inserts; i++)
        {
            list.Insert(ByteSize.Zero, i);
            // The old block is still live when the new one is allocated, so every reallocation yields a new address.
            if (!list.Ptr.Equals(previous))
                reallocations++;
            previous = list.Ptr;
        }

        // 256 bytes with doubling from a small start needs well under 16 reallocations.
        Assert.True(
            reallocations <= 16,
            $"{inserts} inserts caused {reallocations} reallocations; growth is not geometric"
        );
    }

    [Fact]
    public void CapacityThroughINativeList_ReportsCapacityNotSize()
    {
        using var list = new NativeList(ByteSize.Of(16));
        list.Add((byte)1);

        var capacity = ((INativeList)list).Capacity;

        Assert.Equal(16, (int)capacity);
    }

    [Fact]
    public void RemovingARangeThatRunsPastTheEnd_Throws()
    {
        using var list = new NativeList([1, 2, 3, 4]);

        // Only 2 bytes exist from index 2. A numBytes larger than Size cannot be used here:
        // the unsigned Size - numBytes wraps and the library performs a huge Mem.Copy, crashing the host.
        var exception = Record.Exception(() => list.RemoveAt(ByteSize.Of(2), ByteSize.Of(3)));

        Assert.True(
            exception is ArgumentException or IndexOutOfRangeException,
            $"Expected an out-of-range exception but got {exception?.GetType().Name ?? "none"}; Size is now {(int)list.Size}"
        );
    }
}

public class UntypedNativeListAsSpanLikeTests
{
    [Fact]
    public void AddRange_FromAnotherUntypedNativeList_CopiesAllBytes()
    {
        using var source = new NativeList();
        source.Add((byte)1);
        source.Add((byte)2);
        source.Add((byte)3);
        using var destination = new NativeList();

        destination.AddRange(source);

        Assert.Equal(3UL, (ulong)destination.Size);
    }

    [Fact]
    public void StrideList_AddRange_FromUntypedNativeList_CopiesAllBytes()
    {
        using var source = new NativeList();
        source.Add(1);
        source.Add(2);
        using var destination = new NativeStrideList(ByteSize.Of<int>());

        destination.AddRange(source);

        Assert.Equal(2, destination.Count.Value);
    }

    [Fact]
    public void ByteSizeOf_UntypedNativeList_EqualsItsSize()
    {
        using var source = new NativeList();
        source.Add(1L);

        Assert.Equal(source.Size, ByteSize.Of(source));
    }
}
