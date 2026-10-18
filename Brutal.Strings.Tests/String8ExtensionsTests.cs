namespace Brutal.Strings.Tests;

public class String8ExtensionsTests
{
    [Fact]
    public void Compare_TwoEmptyStrings_IsZero()
    {
        Assert.Equal(0, string.CompareOrdinal("", ""));
        Assert.Equal(0, String8.Empty.Compare(""u8));
    }

    [Fact]
    public void Compare_WithIndices_ZeroLengthAgainstEmpty_IsZero()
    {
        Assert.Equal(0, string.CompareOrdinal("abc", 0, "", 0, 0));
        Assert.Equal(0, new String8("abc"u8).Compare(0, ""u8, 0, 0));
    }
}
