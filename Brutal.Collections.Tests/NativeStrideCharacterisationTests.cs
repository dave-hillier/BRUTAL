namespace Brutal.Collections.Tests;

/// <summary>
/// Behaviour of <see cref="NativeStrideList"/> that any rewrite must keep.
/// </summary>
public class NativeStrideListCharacterisationTests
{
    [Fact]
    public void Constructing_SetsStrideAndCapacityInElements()
    {
        using var list = new NativeStrideList(ByteSize.Of(12), 5);

        Assert.Equal(12UL, (ulong)list.Stride);
        Assert.Equal(5, list.Capacity.Value);
        Assert.Equal(0, list.Count.Value);
        Assert.True(list.IsEmpty);
    }

    [Fact]
    public void ConstructingWithAZeroStride_ThrowsArgumentOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new NativeStrideList(ByteSize.Zero));
    }

    [Fact]
    public void AddingValuesOfTheStride_AppendsThem()
    {
        using var list = new NativeStrideList(ByteSize.Of<int>());

        for (var i = 0; i < 20; i++)
            list.Add(i);

        Assert.Equal(20, list.Count.Value);
        Assert.True(list.Capacity.Value >= 20);
        Assert.Equal(Enumerable.Range(0, 20).ToArray(), list.AsSpan<int>().ToArray());
    }

    [Fact]
    public void AddingAValueOfTheWrongSize_ThrowsArgumentException()
    {
        using var list = new NativeStrideList(ByteSize.Of<int>());

        Assert.Throws<ArgumentException>(() => list.Add(1L));
        Assert.Throws<ArgumentException>(() => list.AddRange<long>([1L]));
        Assert.Throws<ArgumentException>(() => list.Insert(0, (short)1));
        Assert.True(list.IsEmpty);
    }

    [Fact]
    public void AddRangeOfBytes_RequiresAMultipleOfTheStride()
    {
        using var list = new NativeStrideList(ByteSize.Of<short>());

        list.AddRange((ReadOnlySpan<byte>)[1, 0, 2, 0]);
        list.AddRange(new byte[] { 3, 0 }.AsSpan());

        Assert.Equal(new short[] { 1, 2, 3 }, list.AsSpan<short>().ToArray());
        Assert.Throws<ArgumentException>(() => list.AddRange((ReadOnlySpan<byte>)[1, 2, 3]));
    }

    [Fact]
    public void AddRangeOfTypedSpansAndSpanLikes_AppendsThem()
    {
        int[] tail = [5, 6];
        using var list = new NativeStrideList(ByteSize.Of<int>());

        list.AddRange<int>([1, 2]);
        list.AddRange(new[] { 3, 4 }.AsSpan());
        list.AddRange(NativeStrideSpan.FromSpan<int>(tail));

        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6 }, list.AsSpan<int>().ToArray());
    }

    [Fact]
    public void FromSpan_CopiesWithTheGivenOrInferredStride()
    {
        int[] data = [1, 2, 3];

        using var inferred = NativeStrideList.FromSpan<int>(data);
        using var explicitStride = NativeStrideList.FromSpan(ByteSize.Of(4), (ReadOnlySpan<int>)[1, 2]);
        using var fromSpanLike = NativeStrideList.FromSpan(NativeStrideSpan.FromSpan<int>(data));
        using var fromSpanLikeWithStride = NativeStrideList.FromSpan(ByteSize.Of(2), NativeStrideSpan.FromSpan<int>(data));

        Assert.Equal(3, inferred.Count.Value);
        Assert.Equal(2, explicitStride.Count.Value);
        Assert.Equal(4UL, (ulong)fromSpanLike.Stride);
        Assert.Equal(data, fromSpanLike.AsSpan<int>().ToArray());
        Assert.Equal(6, fromSpanLikeWithStride.Count.Value);
        Assert.Throws<ArgumentException>(() => NativeStrideList.FromSpan(ByteSize.Of(8), (ReadOnlySpan<int>)[1, 2]));
    }

    [Fact]
    public void InsertingWithSpareCapacity_AndRemoving_ShiftElements()
    {
        using var list = new NativeStrideList(ByteSize.Of<int>(), 8);
        list.Add(1);
        list.Add(3);

        list.Insert(1, 2);
        list.Insert(0, 0);
        list.RemoveAt<int>(3);

        Assert.Equal(new[] { 0, 1, 2 }, list.AsSpan<int>().ToArray());
        Assert.Throws<ArgumentException>(() => list.RemoveAt<long>(0));
        Assert.Throws<IndexOutOfRangeException>(() => list.RemoveAt<int>(3));
    }

    [Fact]
    public void Count_GrowsWithZeroFill()
    {
        using var list = new NativeStrideList(ByteSize.Of<int>());
        list.Add(7);

        list.Count = 3;

        Assert.Equal(new[] { 7, 0, 0 }, list.AsSpan<int>().ToArray());
    }

    [Fact]
    public void Capacity_ClearTrimAndEnsure()
    {
        using var list = new NativeStrideList(ByteSize.Of<int>());
        list.Add(1);
        list.Add(2);

        list.Capacity = 10;
        Assert.Equal(10, list.Capacity.Value);

        list.TrimExcess();
        Assert.Equal(2, list.Capacity.Value);

        list.EnsureCapacity(7);
        Assert.True(list.Capacity.Value >= 7);

        list.Clear();
        Assert.True(list.IsEmpty);
        Assert.True(list.Capacity.Value >= 7);
    }

    [Fact]
    public void AsNativeSpan_AndSpanLike_DescribeTheElements()
    {
        using var list = NativeStrideList.FromSpan<int>([1, 2, 3]);

        var span = list.AsNativeSpan();

        Assert.Equal(3, span.Length.Value);
        Assert.Equal(4UL, (ulong)span.Stride);
        Assert.Equal(3, ((ISpanLike)list).Length.Value);
        Assert.Equal(12UL, (ulong)ByteSize.Of(list));
        Assert.Equal(12, list.AsSpan<byte>().Length);
    }

    [Fact]
    public void FreeAndDisposeTwice_AreSafe()
    {
        var list = NativeStrideList.FromSpan<int>([1, 2, 3]);

        list.Free();
        Assert.True(list.IsNull());
        list.Add(4);
        Assert.Equal(new[] { 4 }, list.AsSpan<int>().ToArray());

        list.Dispose();
        list.Dispose();
        Assert.True(list.IsNull());
    }
}

/// <summary>
/// Behaviour of <see cref="NativeStrideSpan"/> that any rewrite must keep.
/// </summary>
public class NativeStrideSpanCharacterisationTests
{
    [Fact]
    public void FromSpan_DescribesTheMemory()
    {
        int[] data = [1, 2, 3, 4];

        var span = NativeStrideSpan.FromSpan<int>(data);

        Assert.Equal(4, span.Length.Value);
        Assert.Equal(4UL, (ulong)span.Stride);
        Assert.Equal(16UL, (ulong)span.ByteSize);
        Assert.False(span.IsEmpty);
        Assert.False(span.IsNull());
        Assert.Equal(data, span.AsSpan<int>().ToArray());
        Assert.Equal(16, span.AsSpan<byte>().Length);
    }

    [Fact]
    public void FromMutableSpan_DescribesTheMemory()
    {
        var data = new short[] { 1, 2, 3 };

        var span = NativeStrideSpan.FromSpan(data.AsSpan());

        Assert.Equal(3, span.Length.Value);
        Assert.Equal(2UL, (ulong)span.Stride);
    }

    [Fact]
    public void AsSpanOfTheWrongType_ThrowsInvalidOperation()
    {
        int[] data = [1, 2];
        var span = NativeStrideSpan.FromSpan<int>(data);

        var threw = false;
        try
        {
            span.AsSpan<long>();
        }
        catch (InvalidOperationException)
        {
            threw = true;
        }

        Assert.True(threw);
    }

    [Fact]
    public void Slicing_ByStartAndLengthOrStartAndEnd()
    {
        int[] data = [1, 2, 3, 4, 5];
        var span = NativeStrideSpan.FromSpan<int>(data);

        Assert.Equal(new[] { 2, 3 }, span.Slice(1, 2).AsSpan<int>().ToArray());
        Assert.Equal(new[] { 3, 4, 5 }, span[2, 5].AsSpan<int>().ToArray());
        Assert.Equal(new[] { 2, 3, 4 }, span[1..4].AsSpan<int>().ToArray());
        Assert.True(span[2..2].IsEmpty);
    }

    [Fact]
    public void WithStride_ReinterpretsTheBytes()
    {
        int[] data = [1, 2, 3, 4];
        var span = NativeStrideSpan.FromSpan<int>(data);

        var asLongs = span.WithStride(ByteSize.Of(8));

        Assert.Equal(2, asLongs.Length.Value);
        Assert.Equal(16UL, (ulong)asLongs.ByteSize);
    }

    [Fact]
    public void WithAZeroStride_ThrowsArgumentOutOfRange()
    {
        int[] data = [1];
        var span = NativeStrideSpan.FromSpan<int>(data);

        var threw = false;
        try
        {
            span.WithStride(ByteSize.Zero);
        }
        catch (ArgumentOutOfRangeException)
        {
            threw = true;
        }

        Assert.True(threw);
    }
}
