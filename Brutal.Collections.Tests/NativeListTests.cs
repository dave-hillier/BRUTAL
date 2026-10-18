namespace Brutal.Collections.Tests;

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
