namespace Brutal.Core.Tests;

public class ByteSizeTests
{
    [Fact]
    public void ByteSize64_implicit_int_conversion_does_not_silently_truncate()
    {
        var fiveGb = new ByteSize64(5UL << 30);
        var ex = Record.Exception(() =>
        {
            int asInt = fiveGb; // implicit
            Assert.Equal(5L << 30, (long)asInt);
        });
        Assert.IsType<OverflowException>(ex);
    }

    [Fact]
    public void ByteSize32_implicit_int_conversion_does_not_silently_truncate()
    {
        var threeGb = new ByteSize32(3u << 30);
        var ex = Record.Exception(() =>
        {
            int asInt = threeGb; // implicit
            Assert.Equal(3L << 30, (long)asInt);
        });
        Assert.IsType<OverflowException>(ex);
    }
}
