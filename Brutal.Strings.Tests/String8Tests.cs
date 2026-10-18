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
}
