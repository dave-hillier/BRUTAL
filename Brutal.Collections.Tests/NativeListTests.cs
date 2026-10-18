namespace Brutal.Collections.Tests;

/// <summary>
/// Tests for the untyped, byte-addressed <see cref="NativeList"/>.
/// </summary>
public class UntypedNativeListTests
{
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
