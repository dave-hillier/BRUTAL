namespace Brutal.Collections.Tests;

/// <summary>
/// Tests for the untyped, byte-addressed <see cref="NativeList"/>.
/// </summary>
public class UntypedNativeListTests
{
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
}
