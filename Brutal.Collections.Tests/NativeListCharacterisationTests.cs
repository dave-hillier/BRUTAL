namespace Brutal.Collections.Tests;

public class NativeListCharacterisationTests
{
    [Fact]
    public void NewList_IsEmptyAndNull()
    {
        using var list = new NativeList();

        Assert.True(list.IsEmpty);
        Assert.True(list.IsNull());
        Assert.Equal(0UL, (ulong)list.Size);
        Assert.Equal(0UL, (ulong)list.Capacity);
    }

    [Fact]
    public void ConstructingWithACapacity_AllocatesExactlyThatCapacity()
    {
        using var list = new NativeList(ByteSize.Of(32));

        Assert.Equal(32UL, (ulong)list.Capacity);
        Assert.Equal(0UL, (ulong)list.Size);
        Assert.False(list.IsNull());
    }

    [Fact]
    public void ConstructingFromBytes_CopiesThem()
    {
        byte[] data = [1, 2, 3, 4, 5];
        using var list = new NativeList(data);

        data[0] = 99;

        Assert.Equal(5UL, (ulong)list.Size);
        Assert.True(list.Capacity >= list.Size);
        Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, list.AsSpan<byte>().ToArray());
    }

    [Fact]
    public void AddingBytes_AppendsThemInOrder()
    {
        using var list = new NativeList();

        for (var i = 0; i < 100; i++)
            list.Add((byte)i);

        Assert.Equal(100UL, (ulong)list.Size);
        Assert.True(list.Capacity >= list.Size);
        Assert.Equal(Enumerable.Range(0, 100).Select(i => (byte)i).ToArray(), list.AsSpan<byte>().ToArray());
    }

    [Fact]
    public void AddingTypedValues_AppendsTheirBytes()
    {
        using var list = new NativeList();

        for (var i = 0; i < 50; i++)
            list.Add(i * 3);

        Assert.Equal(200UL, (ulong)list.Size);
        Assert.True(list.Capacity >= list.Size);
        Assert.Equal(Enumerable.Range(0, 50).Select(i => i * 3).ToArray(), list.AsSpan<int>().ToArray());
    }

    [Fact]
    public void AddRangeOfTypedSpan_AppendsAllItems()
    {
        using var list = new NativeList();
        list.Add((byte)7);

        list.AddRange<short>([1, 2, 3]);

        Assert.Equal(7UL, (ulong)list.Size);
        Assert.Equal(new byte[] { 7, 1, 0, 2, 0, 3, 0 }, list.AsSpan<byte>().ToArray());
    }

    [Fact]
    public void AddRangeOfSpanLike_AppendsItsBytes()
    {
        int[] data = [10, 20, 30];
        using var list = new NativeList();

        list.AddRange(NativeStrideSpan.FromSpan<int>(data));

        Assert.Equal(12UL, (ulong)list.Size);
        Assert.Equal(data, list.AsSpan<int>().ToArray());
    }

    [Fact]
    public void InsertingATypedValueWithSpareCapacity_ShiftsLaterBytes()
    {
        using var list = new NativeList(ByteSize.Of(32));
        list.Add(1);
        list.Add(3);

        list.Insert(ByteSize.Of(4), 2);
        list.Insert(ByteSize.Of(12), 4);
        list.Insert(ByteSize.Zero, 0);

        Assert.Equal(20UL, (ulong)list.Size);
        Assert.Equal(new[] { 0, 1, 2, 3, 4 }, list.AsSpan<int>().ToArray());
    }

    [Fact]
    public void InsertingPastTheEnd_ThrowsIndexOutOfRange()
    {
        using var list = new NativeList(ByteSize.Of(16));
        list.Add(1);

        Assert.Throws<IndexOutOfRangeException>(() => list.Insert(ByteSize.Of(5), 2));
        Assert.Throws<IndexOutOfRangeException>(() => list.Insert(ByteSize.Of(5), (byte)2));
        Assert.Equal(4UL, (ulong)list.Size);
    }

    [Fact]
    public void RemovingAByte_ShiftsLaterBytesDown()
    {
        using var list = new NativeList([1, 2, 3, 4]);

        list.RemoveAt(ByteSize.Of(1));

        Assert.Equal(new byte[] { 1, 3, 4 }, list.AsSpan<byte>().ToArray());
    }

    [Fact]
    public void RemovingTheLastByte_ShrinksSize()
    {
        using var list = new NativeList([1, 2, 3, 4]);

        list.RemoveAt(ByteSize.Of(3));

        Assert.Equal(new byte[] { 1, 2, 3 }, list.AsSpan<byte>().ToArray());
    }

    [Fact]
    public void RemovingARangeThatFits_ShiftsLaterBytesDown()
    {
        using var list = new NativeList([1, 2, 3, 4, 5, 6]);

        list.RemoveAt(ByteSize.Of(1), ByteSize.Of(3));

        Assert.Equal(new byte[] { 1, 5, 6 }, list.AsSpan<byte>().ToArray());
    }

    [Fact]
    public void RemovingARangeEndingAtTheEnd_Truncates()
    {
        using var list = new NativeList([1, 2, 3, 4, 5, 6]);

        list.RemoveAt(ByteSize.Of(2), ByteSize.Of(4));

        Assert.Equal(new byte[] { 1, 2 }, list.AsSpan<byte>().ToArray());
    }

    [Fact]
    public void RemovingAtOrPastTheEnd_ThrowsIndexOutOfRange()
    {
        using var list = new NativeList([1, 2, 3]);

        Assert.Throws<IndexOutOfRangeException>(() => list.RemoveAt(ByteSize.Of(3)));
        Assert.Throws<IndexOutOfRangeException>(() => list.RemoveAt(ByteSize.Of(10), ByteSize.One));
        Assert.Equal(3UL, (ulong)list.Size);
    }

    [Fact]
    public void Clear_ResetsSizeAndKeepsCapacity()
    {
        using var list = new NativeList([1, 2, 3, 4]);
        var capacity = list.Capacity;

        list.Clear();

        Assert.True(list.IsEmpty);
        Assert.Equal(capacity, list.Capacity);
        Assert.False(list.IsNull());
    }

    [Fact]
    public void GrowingSize_ZeroFillsNewBytes()
    {
        using var list = new NativeList([1, 2]);
        list.Size = ByteSize.Of(1);
        list.Capacity = ByteSize.Of(8);
        // Dirty the memory beyond Size, then grow over it.
        list.AddRange<byte>([9, 9, 9, 9, 9, 9, 9]);
        list.Size = ByteSize.Of(1);

        list.Size = ByteSize.Of(20);

        Assert.True(list.Capacity >= list.Size);
        Assert.Equal(new byte[] { 1 }.Concat(new byte[19]).ToArray(), list.AsSpan<byte>().ToArray());
    }

    [Fact]
    public void ShrinkingSize_KeepsCapacityAndLeadingBytes()
    {
        using var list = new NativeList([1, 2, 3, 4]);

        list.Size = ByteSize.Of(2);

        Assert.Equal(4UL, (ulong)list.Capacity);
        Assert.Equal(new byte[] { 1, 2 }, list.AsSpan<byte>().ToArray());
    }

    [Fact]
    public void SettingCapacity_PreservesContents()
    {
        using var list = new NativeList([1, 2, 3]);

        list.Capacity = ByteSize.Of(64);

        Assert.Equal(64UL, (ulong)list.Capacity);
        Assert.Equal(new byte[] { 1, 2, 3 }, list.AsSpan<byte>().ToArray());
    }

    [Fact]
    public void SettingCapacityBelowSize_ThrowsArgumentOutOfRange()
    {
        using var list = new NativeList([1, 2, 3]);

        Assert.Throws<ArgumentOutOfRangeException>(() => list.Capacity = ByteSize.Of(2));
        Assert.Equal(new byte[] { 1, 2, 3 }, list.AsSpan<byte>().ToArray());
    }

    [Fact]
    public void SettingCapacityToZeroOnEmptyList_ReleasesMemory()
    {
        using var list = new NativeList(ByteSize.Of(16));

        list.Capacity = ByteSize.Zero;

        Assert.True(list.IsNull());
        Assert.Equal(0UL, (ulong)list.Capacity);
    }

    [Fact]
    public void TrimExcess_ShrinksCapacityToSize()
    {
        using var list = new NativeList(ByteSize.Of(64));
        list.Add(1);
        list.Add(2);

        list.TrimExcess();

        Assert.Equal(8UL, (ulong)list.Capacity);
        Assert.Equal(new[] { 1, 2 }, list.AsSpan<int>().ToArray());
    }

    [Fact]
    public void TrimExcessOnEmptyList_ReleasesMemory()
    {
        using var list = new NativeList(ByteSize.Of(64));

        list.TrimExcess();

        Assert.True(list.IsNull());
        Assert.Equal(0UL, (ulong)list.Capacity);
    }

    [Fact]
    public void EnsureCapacity_GrowsToAtLeastTheRequestAndNeverShrinks()
    {
        using var list = new NativeList([1, 2, 3]);

        list.EnsureCapacity(ByteSize.Of(100));
        Assert.True((ulong)list.Capacity >= 100);

        var capacity = list.Capacity;
        list.EnsureCapacity(ByteSize.Of(10));
        Assert.Equal(capacity, list.Capacity);
        Assert.Equal(new byte[] { 1, 2, 3 }, list.AsSpan<byte>().ToArray());
    }

    [Fact]
    public void ByteIndexer_ReadsBytesAndRejectsOutOfRange()
    {
        using var list = new NativeList([5, 6, 7]);

        Assert.Equal(5, list[0]);
        Assert.Equal(7, list[2]);
        Assert.Throws<IndexOutOfRangeException>(() => list[3]);
        Assert.Throws<IndexOutOfRangeException>(() => list[-1]);
    }

    [Fact]
    public void CountThroughINativeList_IsTheSizeInBytes()
    {
        using var list = new NativeList([1, 2, 3]);
        INativeList asInterface = list;

        Assert.Equal(3, asInterface.Count.Value);

        asInterface.Count = 5;

        Assert.Equal(5UL, (ulong)list.Size);
        Assert.Equal(new byte[] { 1, 2, 3, 0, 0 }, list.AsSpan<byte>().ToArray());
    }

    [Fact]
    public void LengthThroughISpanLike_IsTheSizeInBytes()
    {
        using var list = new NativeList([1, 2, 3]);

        Assert.Equal(3, ((ISpanLike)list).Length.Value);
    }

    [Fact]
    public void EnsureCapacityThroughINativeList_IsInBytes()
    {
        using var list = new NativeList();

        ((INativeList)list).EnsureCapacity(40);

        Assert.True((ulong)list.Capacity >= 40);
    }

    [Fact]
    public void Free_ReleasesMemoryAndTheListIsReusable()
    {
        using var list = new NativeList([1, 2, 3]);

        list.Free();

        Assert.True(list.IsNull());
        Assert.True(list.IsEmpty);
        Assert.Equal(0UL, (ulong)list.Capacity);

        list.Add(42);
        Assert.Equal(new[] { 42 }, list.AsSpan<int>().ToArray());
    }

    [Fact]
    public void DisposingTwice_IsSafe()
    {
        var list = new NativeList([1, 2, 3]);

        list.Dispose();
        list.Dispose();

        Assert.True(list.IsNull());
    }
}
