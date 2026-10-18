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

    // Alignment pads by grapheme count, so multi-byte graphemes can need more room than was pre-grown:
    // "éé" is 4 UTF-8 bytes but only 2 graphemes, so alignment 4 needs 6 bytes. With room for 4,
    // the content is written, then the Align retry has to grow and throws.

    [Fact]
    public void ValueStringBuilder_FailedAlign_Formatted_KeepsNullTerminator()
    {
        Span<byte> buffer = stackalloc byte[5]; // capacity 4 + terminator
        var sb = buffer.ToStringBuilder();

        var threw = false;
        try
        {
            sb.Append("\u00e9\u00e9", alignment: 4); // Append<T>(string)
        }
        catch (ArgumentOutOfRangeException)
        {
            threw = true;
        }

        Assert.True(threw);
        Assert.Equal(0, sb.Position);
        Assert.Equal(0, buffer[sb.Position]);
    }

    [Fact]
    public void ValueStringBuilder_FailedAlign_String8View_KeepsNullTerminator()
    {
        Span<byte> buffer = stackalloc byte[5]; // capacity 4 + terminator
        var sb = buffer.ToStringBuilder();

        var threw = false;
        try
        {
            sb.Append("\u00e9\u00e9"u8, alignment: 4);
        }
        catch (ArgumentOutOfRangeException)
        {
            threw = true;
        }

        Assert.True(threw);
        Assert.Equal(0, sb.Position);
        Assert.Equal(0, buffer[sb.Position]);
    }

    [Fact]
    public void ValueStringBuilder_FailedAlignFrom_NestedInterpolation_KeepsNullTerminatorAndPosition()
    {
        Span<byte> buffer = stackalloc byte[9]; // capacity 8 + terminator
        var sb = buffer.ToStringBuilder();
        sb.Append("ab"u8);
        var eAcute = "\u00e9"; // non-constant hole: keeps the inner string a nested handler (AlignFrom), not a constant

        var threw = false;
        try
        {
            // inner handler writes "x\u00e9" (3 bytes, 2 graphemes) directly, then the outer hole pads it to 6 graphemes = 7 bytes
            sb.Append($"{$"x{eAcute}",6}");
        }
        catch (ArgumentOutOfRangeException)
        {
            threw = true;
        }

        Assert.True(threw);
        Assert.Equal(2, sb.Position);
        Assert.Equal(0, buffer[sb.Position]);
    }

    [Fact]
    public void StringBuilder_FailedAlign_AtMaxCapacity_Formatted_KeepsNullTerminator()
    {
        var sb = new StringBuilder<byte>(capacity: 4, maxCapacity: 4);

        Assert.Throws<ArgumentOutOfRangeException>(() => sb.Append("\u00e9\u00e9", alignment: 4));

        Assert.Equal(0, sb.Position);
        Assert.Equal(0, ElementAfterWritten(sb));
    }

    [Fact]
    public void StringBuilder_FailedAlign_AtMaxCapacity_String16View_KeepsNullTerminator()
    {
        var sb = new StringBuilder<byte>(capacity: 4, maxCapacity: 4);

        Assert.Throws<ArgumentOutOfRangeException>(() => sb.Append("\u00e9\u00e9".AsSpan(), alignment: 4));

        Assert.Equal(0, sb.Position);
        Assert.Equal(0, ElementAfterWritten(sb));
    }

    [Fact]
    public void StringBuilder_FailedAlign_AtMaxCapacity_String8View_KeepsNullTerminator()
    {
        var sb = new StringBuilder<byte>(capacity: 4, maxCapacity: 4);

        Assert.Throws<ArgumentOutOfRangeException>(() => sb.Append("\u00e9\u00e9"u8, alignment: 4));

        Assert.Equal(0, sb.Position);
        Assert.Equal(0, ElementAfterWritten(sb));
    }

    [Fact]
    public void StringBuilder_FailedAlignFrom_NestedInterpolation_KeepsNullTerminatorAndPosition()
    {
        var sb = new StringBuilder<byte>(capacity: 8, maxCapacity: 8);
        sb.Append("ab"u8);
        var eAcute = "\u00e9"; // non-constant hole: keeps the inner string a nested handler (AlignFrom), not a constant

        // inner handler writes "x\u00e9" (3 bytes, 2 graphemes) directly, then the outer hole pads it to 6 graphemes = 7 bytes
        Assert.Throws<ArgumentOutOfRangeException>(() => sb.Append($"{$"x{eAcute}",6}"));

        Assert.Equal(2, sb.Position);
        Assert.Equal(0, ElementAfterWritten(sb));
    }

    private static byte ElementAfterWritten(StringBuilder<byte> sb) =>
        Unsafe.Add(ref MemoryMarshal.GetReference(sb.WrittenSpan), sb.Position);
}
