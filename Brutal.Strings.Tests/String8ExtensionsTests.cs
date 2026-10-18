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

    [Fact]
    public void LastIndexOf_Char_WithStartIndex_SearchesBackwardsFromStartIndex()
    {
        Assert.Equal(3, "abcabc".LastIndexOf('a', 4));
        var s = new String8("abcabc"u8);
        Assert.Equal(3, s.LastIndexOf((byte)'a', 4));
    }

    [Fact]
    public void LastIndexOf_Char_WithStartIndex_IgnoresMatchesAfterStartIndex()
    {
        Assert.Equal(0, "abcabc".LastIndexOf('a', 2));
        var s = new String8("abcabc"u8);
        Assert.Equal(0, s.LastIndexOf((byte)'a', 2));
    }

    [Fact]
    public void LastIndexOf_Char_WithStartIndexAndCount_SearchesBackwardsFromStartIndex()
    {
        Assert.Equal(0, "abcabc".LastIndexOf('a', 2, 3));
        var s = new String8("abcabc"u8);
        Assert.Equal(0, s.LastIndexOf((byte)'a', 2, 3));
    }

    [Fact]
    public void LastIndexOf_Char_WithStartIndexAndCount_IgnoresMatchesBeforeCountWindow()
    {
        Assert.Equal(-1, "abcabc".LastIndexOf('a', 5, 2));
        var s = new String8("abcabc"u8);
        Assert.Equal(-1, s.LastIndexOf((byte)'a', 5, 2));
    }

    [Fact]
    public void LastIndexOf_Char_OnEmptyString_ReturnsMinusOne()
    {
        Assert.Equal(-1, "".LastIndexOf('a', 0));
        Assert.Equal(-1, "".LastIndexOf('a', -1));
        Assert.Equal(-1, "".LastIndexOf('a', 0, 0));
        Assert.Equal(-1, String8.Empty.LastIndexOf((byte)'a', 0));
        Assert.Equal(-1, String8.Empty.LastIndexOf((byte)'a', -1));
        Assert.Equal(-1, String8.Empty.LastIndexOf((byte)'a', 0, 0));
    }

    [Fact]
    public void LastIndexOfAny_OnEmptyString_ReturnsMinusOne()
    {
        Assert.Equal(-1, "".LastIndexOfAny(['a'], 0));
        Assert.Equal(-1, "".LastIndexOfAny(['a'], 0, 0));
        Assert.Equal(-1, String8.Empty.LastIndexOfAny("a"u8, 0));
        Assert.Equal(-1, String8.Empty.LastIndexOfAny("a"u8, 0, 0));
    }
}
