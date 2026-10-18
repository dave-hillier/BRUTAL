using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Brutal.Strings.Tests;

public class StringBuilderTests
{
    // Doc (StringBuilder.WrittenSpan): "The element immediately after this span is always the null-terminator."
    // TryAppend paths call Fail() to restore it after a partial write; the throwing Append<T> path does not.

    [Fact]
    public void ValueStringBuilder_FailedAppend_KeepsNullTerminator()
    {
        Span<byte> buffer = stackalloc byte[4]; // capacity 3 + terminator
        var sb = buffer.ToStringBuilder();

        var threw = false;
        try
        {
            sb.Append("toolong"); // binds to Append<T>(string): partial UTF-8 write, then GetSpan throws
        }
        catch (ArgumentOutOfRangeException)
        {
            threw = true;
        }

        Assert.True(threw);
        Assert.Equal(0, sb.Position);
        Assert.Equal(0, buffer[sb.Position]); // the element after WrittenSpan must be the null terminator
    }

    [Fact]
    public void StringBuilder_FailedAppend_AtMaxCapacity_KeepsNullTerminator()
    {
        var sb = new StringBuilder<byte>(capacity: 4, maxCapacity: 4);
        sb.Append("ab"u8);

        Assert.Throws<ArgumentOutOfRangeException>(() => sb.Append("cdefgh"));

        Assert.Equal(2, sb.Position);
        var terminator = Unsafe.Add(ref MemoryMarshal.GetReference(sb.WrittenSpan), sb.Position);
        Assert.Equal(0, terminator);
    }
}
