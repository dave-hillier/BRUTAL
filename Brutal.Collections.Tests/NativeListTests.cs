namespace Brutal.Collections.Tests;

/// <summary>
/// Tests for the untyped, byte-addressed <see cref="NativeList"/>.
/// </summary>
public class UntypedNativeListTests
{
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
}
