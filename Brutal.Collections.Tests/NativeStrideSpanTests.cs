namespace Brutal.Collections.Tests;

public class NativeStrideSpanRangeTests
{
    [Fact]
    public void FullRange_ReturnsWholeSpan()
    {
        int[] data = [1, 2, 3, 4, 5];
        var span = NativeStrideSpan.FromSpan<int>(data);

        var all = span[..];

        Assert.Equal(5, all.Length.Value);
    }

    [Fact]
    public void FromEndRange_ExcludesLastElement()
    {
        int[] data = [1, 2, 3, 4, 5];
        var span = NativeStrideSpan.FromSpan<int>(data);

        var middle = span[1..^1];

        Assert.Equal(3, middle.Length.Value);
        Assert.Equal([2, 3, 4], middle.AsSpan<int>().ToArray());
    }

    [Fact]
    public void OpenEndedRange_RunsToEnd()
    {
        int[] data = [1, 2, 3, 4, 5];
        var span = NativeStrideSpan.FromSpan<int>(data);

        var tail = span[2..];

        Assert.Equal(3, tail.Length.Value);
    }
}
