namespace Brutal.Strings.Tests;

public class String8Tests
{
    [Fact]
    public void String8_Move_EmptyArray_ReturnsEmpty()
    {
        var s = String8.Move([]);
        Assert.True(s.IsEmpty);
    }

    [Fact]
    public void Utf8Path_Combine_TwoEmptyPaths_ReturnsEmpty_LikeBcl()
    {
        Assert.Equal("", Path.Combine("", ""));
        var s = Utf8.Path.Combine(""u8, ""u8);
        Assert.True(s.IsEmpty);
    }

    [Fact]
    public void Utf8Path_Join_TwoEmptyPaths_ReturnsEmpty_LikeBcl()
    {
        Assert.Equal("", Path.Join("", ""));
        var s = Utf8.Path.Join(""u8, ""u8);
        Assert.True(s.IsEmpty);
    }
}
