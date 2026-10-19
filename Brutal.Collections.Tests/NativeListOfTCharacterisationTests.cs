namespace Brutal.Collections.Tests;

public class NativeListOfTCharacterisationTests
{
    private struct Vertex
    {
        public float X;
        public float Y;
        public float Z;
    }

    [Fact]
    public void NewList_IsEmptyAndHasTheElementStride()
    {
        using var list = new NativeList<Vertex>();

        Assert.True(list.IsEmpty);
        Assert.Equal(0, list.Count.Value);
        Assert.Equal(12UL, (ulong)list.Stride);
        Assert.Equal(12UL, (ulong)((ISpanLike)list).Stride);
    }

    [Fact]
    public void ConstructingWithACapacity_ReservesThatManyElements()
    {
        using var list = new NativeList<int>(10);

        Assert.Equal(10, list.Capacity.Value);
        Assert.Equal(0, list.Count.Value);
    }

    [Fact]
    public void ConstructingFromASpan_CopiesTheElements()
    {
        int[] data = [1, 2, 3];
        using var list = new NativeList<int>(data);

        data[0] = 99;

        Assert.Equal(3, list.Count.Value);
        Assert.Equal(new[] { 1, 2, 3 }, list.AsSpan().ToArray());
    }

    [Fact]
    public void CollectionExpression_BuildsAList()
    {
        using NativeList<int> list = [4, 5, 6];

        Assert.Equal(new[] { 4, 5, 6 }, list.AsSpan().ToArray());
    }

    [Fact]
    public void Adding_AppendsElementsAndKeepsCapacityAboveCount()
    {
        using var list = new NativeList<Vertex>();

        for (var i = 0; i < 100; i++)
        {
            list.Add(new Vertex { X = i, Y = -i, Z = 2 * i });
            Assert.True(list.Capacity.Value >= list.Count.Value);
        }

        Assert.Equal(100, list.Count.Value);
        Assert.Equal(42f, list[42].X);
        Assert.Equal(-99f, list[99].Y);
    }

    [Fact]
    public void AddRange_AppendsAllElements()
    {
        using var list = new NativeList<int>();
        using var other = new NativeList<int>([5, 6]);
        list.Add(0);

        list.AddRange((ReadOnlySpan<int>)[1, 2]);
        list.AddRange(new[] { 3, 4 }.AsSpan());
        list.AddRange(other);

        Assert.Equal(new[] { 0, 1, 2, 3, 4, 5, 6 }, list.AsSpan().ToArray());
    }

    [Fact]
    public void InsertingWithSpareCapacity_ShiftsLaterElements()
    {
        using var list = new NativeList<int>(8);
        list.Add(1);
        list.Add(3);

        list.Insert(1, 2);
        list.Insert(3, 4);
        list.Insert(0, 0);

        Assert.Equal(new[] { 0, 1, 2, 3, 4 }, list.AsSpan().ToArray());
    }

    [Fact]
    public void InsertingPastTheEnd_ThrowsIndexOutOfRange()
    {
        using var list = new NativeList<int>(8);
        list.Add(1);

        Assert.Throws<IndexOutOfRangeException>(() => list.Insert(2, 5));
    }

    [Fact]
    public void RemoveAt_ShiftsLaterElementsDown()
    {
        using var list = new NativeList<int>([1, 2, 3, 4]);

        list.RemoveAt(1);
        list.RemoveAt(2);

        Assert.Equal(new[] { 1, 3 }, list.AsSpan().ToArray());
    }

    [Fact]
    public void RemoveAtTheEnd_ThrowsIndexOutOfRange()
    {
        using var list = new NativeList<int>([1, 2]);

        Assert.Throws<IndexOutOfRangeException>(() => list.RemoveAt(2));
        Assert.Equal(2, list.Count.Value);
    }

    [Fact]
    public void Indexer_ReturnsAWritableReferenceAndSupportsFromEnd()
    {
        using var list = new NativeList<int>([1, 2, 3]);

        list[0] = 10;
        list[^1] = 30;
        ref var middle = ref list[1];
        middle = 20;

        Assert.Equal(new[] { 10, 20, 30 }, list.AsSpan().ToArray());
        Assert.Equal(20, list[^2]);
    }

    [Fact]
    public void Indexer_OutOfRange_ThrowsIndexOutOfRange()
    {
        using var list = new NativeList<int>([1, 2, 3]);

        Assert.Throws<IndexOutOfRangeException>(() => list[3]);
        Assert.Throws<IndexOutOfRangeException>(() => list[^4]);
    }

    [Fact]
    public void RangeIndexer_AndSlices_ViewTheElements()
    {
        using var list = new NativeList<int>([1, 2, 3, 4, 5]);

        Assert.Equal(new[] { 2, 3, 4 }, list[1..^1].ToArray());
        Assert.Equal(new[] { 4, 5 }, list[^2..].ToArray());
        Assert.Equal(new[] { 2, 3 }, list.Slice(1, 2).ToArray());
        Assert.Equal(new[] { 3, 4, 5 }, list.Slice(2).ToArray());

        list[1..3][0] = 20;
        Assert.Equal(20, list[1]);
    }

    [Fact]
    public void SliceOutOfRange_ThrowsArgumentOutOfRange()
    {
        using var list = new NativeList<int>([1, 2, 3]);

        Assert.Throws<ArgumentOutOfRangeException>(() => list.Slice(2, 2));
    }

    [Fact]
    public void GrowingCount_ZeroFillsNewElements()
    {
        using var list = new NativeList<int>([1, 2, 3, 4]);
        list.Count = 1;

        list.Count = 6;

        Assert.Equal(new[] { 1, 0, 0, 0, 0, 0 }, list.AsSpan().ToArray());
        Assert.True(list.Capacity.Value >= 6);
    }

    [Fact]
    public void Clear_KeepsCapacity()
    {
        using var list = new NativeList<int>([1, 2, 3]);
        var capacity = list.Capacity;

        list.Clear();

        Assert.True(list.IsEmpty);
        Assert.Equal(capacity, list.Capacity);
    }

    [Fact]
    public void Capacity_CanBeSetAndTrimmed()
    {
        using var list = new NativeList<int>([1, 2, 3]);

        list.Capacity = 20;
        Assert.Equal(20, list.Capacity.Value);

        list.TrimExcess();
        Assert.Equal(3, list.Capacity.Value);

        list.EnsureCapacity(9);
        Assert.True(list.Capacity.Value >= 9);
        Assert.Equal(new[] { 1, 2, 3 }, list.AsSpan().ToArray());
    }

    [Fact]
    public void SettingCapacityBelowCount_ThrowsArgumentOutOfRange()
    {
        using var list = new NativeList<int>([1, 2, 3]);

        Assert.Throws<ArgumentOutOfRangeException>(() => list.Capacity = 2);
    }

    [Fact]
    public void Enumerating_YieldsElementsInOrder()
    {
        using var list = new NativeList<int>([3, 1, 2]);
        var seen = new List<int>();

        foreach (var value in list)
            seen.Add(value);

        Assert.Equal(new[] { 3, 1, 2 }, seen);
        Assert.Equal(new[] { 3, 1, 2 }, ((IEnumerable<int>)list).ToArray());
        Assert.Equal(6, list.Sum());
    }

    [Fact]
    public void EnumeratingAnEmptyList_YieldsNothing()
    {
        using var list = new NativeList<int>();

        Assert.Empty(list);
    }

    [Fact]
    public unsafe void Pinning_PointsAtTheFirstElement()
    {
        using var list = new NativeList<int>([7, 8]);

        fixed (int* p = list)
        {
            Assert.True(p == list.Ptr.Pointer);
            Assert.Equal(7, p[0]);
            Assert.Equal(8, p[1]);
        }
    }

    [Fact]
    public void SpanLikeViews_ExposeTheSameMemory()
    {
        using var list = new NativeList<int>([1, 2, 3]);
        ISpanLike asSpanLike = list;

        Assert.Equal(3, asSpanLike.Length.Value);
        Assert.Equal(12, asSpanLike.AsSpan<byte>().Length);
        Assert.Equal(12UL, (ulong)ByteSize.Of(list));

        var native = list.AsNativeSpan();
        Assert.Equal(3, native.Length.Value);
        Assert.Equal(4UL, (ulong)native.Stride);
        Assert.Equal(new[] { 1, 2, 3 }, native.AsSpan<int>().ToArray());
    }

    [Fact]
    public void Free_ReleasesMemoryAndTheListIsReusable()
    {
        using var list = new NativeList<int>([1, 2, 3]);

        list.Free();
        Assert.True(list.IsNull());
        Assert.True(list.IsEmpty);

        list.Add(5);
        Assert.Equal(new[] { 5 }, list.AsSpan().ToArray());
    }

    [Fact]
    public void DisposingTwice_IsSafe()
    {
        var list = new NativeList<int>([1, 2, 3]);

        list.Dispose();
        list.Dispose();

        Assert.True(list.IsNull());
    }
}
