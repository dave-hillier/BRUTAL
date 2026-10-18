using System.Text;

namespace Brutal.Strings.Tests;

public class String8Tests
{
    [Fact]
    public void StringBuilder8_Append_String8_AppendsItsContent()
    {
        var s8 = new String8("hello"u8);
        var sb = new StringBuilder<byte>();
        sb.Append(s8);
        Assert.Equal("hello", Encoding.UTF8.GetString(sb.WrittenSpan));
    }

    [Fact]
    public void StringBuilder8_Interpolated_String8Hole_AppendsItsContent()
    {
        var s8 = new String8("hello"u8);
        var sb = new StringBuilder<byte>();
        sb.Append($"[{s8}]");
        Assert.Equal("[hello]", Encoding.UTF8.GetString(sb.WrittenSpan));
    }

    [Fact]
    public void StringBuilder16_Append_String8_AppendsItsContent()
    {
        var s8 = new String8("hello"u8);
        var sb = new StringBuilder<char>();
        sb.Append(s8);
        Assert.Equal("hello", sb.WrittenSpan.ToString());
    }

    [Fact]
    public void StringBuilder16_Interpolated_String8Hole_AppendsItsContent()
    {
        var s8 = new String8("hello"u8);
        var sb = new StringBuilder<char>();
        sb.Append($"[{s8}]");
        Assert.Equal("[hello]", sb.WrittenSpan.ToString());
    }

    [Fact]
    public void StringInterpolation_String8Hole_FormatsItsContent()
    {
        var s8 = new String8("hello"u8);
        Assert.Equal("[hello]", $"[{s8}]");
    }

    [Fact]
    public void ToString_ReturnsItsContent()
    {
        var s8 = new String8("hello"u8);
        Assert.Equal("hello", s8.ToString());
    }

    [Fact]
    public void ToString_Empty_ReturnsEmptyString()
    {
        Assert.Equal("", String8.Empty.ToString());
    }

    [Fact]
    public void StringBuilder16_Append_EmptyString8_AppendsNothing()
    {
        var sb = new StringBuilder<char>();
        sb.Append(String8.Empty);
        Assert.Equal("", sb.WrittenSpan.ToString());
    }

    [Fact]
    public void StringBuilder8_Append_EmptyString8_AppendsNothing()
    {
        var sb = new StringBuilder<byte>();
        sb.Append(String8.Empty);
        Assert.Equal("", Encoding.UTF8.GetString(sb.WrittenSpan));
    }

    [Fact]
    public void StringBuilder16_Append_String8LongerThanInitialCapacity_AppendsItsContent()
    {
        var text = new string('x', 1000);
        var s8 = new String8(text);
        var sb = new StringBuilder<char>();
        sb.Append(s8);
        Assert.Equal(text, sb.WrittenSpan.ToString());
    }

    [Fact]
    public void StringBuilder8_Append_String8LongerThanInitialCapacity_AppendsItsContent()
    {
        var text = new string('x', 1000);
        var s8 = new String8(text);
        var sb = new StringBuilder<byte>();
        sb.Append(s8);
        Assert.Equal(text, Encoding.UTF8.GetString(sb.WrittenSpan));
    }
}
