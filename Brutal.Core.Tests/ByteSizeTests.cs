namespace Brutal.Core.Tests;

public class ByteSizeTests
{
    [Fact]
    public void ByteSize_ToElementCount_never_returns_negative_count()
    {
        var threeGb = new ByteSize((nuint)(3UL << 30));
        var ex = Record.Exception(() => threeGb.ToElementCount<byte>());
        if (ex is null)
            Assert.True(threeGb.ToElementCount<byte>().Value >= 0, $"got {threeGb.ToElementCount<byte>().Value}");
        else
            Assert.IsType<OverflowException>(ex);
    }
}
