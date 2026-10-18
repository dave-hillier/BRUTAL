namespace Brutal.Core.Tests;

public class ByteSizeTests
{
    [Theory]
    [InlineData(1048575UL)] // 1 MiB - 1
    [InlineData(1099511627775UL)] // 1 TiB - 1
    public void ByteSize_ToString_never_shows_1024_of_a_unit(ulong bytes)
    {
        var s = new ByteSize((nuint)bytes).ToString();
        Assert.DoesNotContain("1024 ", s);
    }
}
