namespace Brutal.Strings.Tests;

public class RefString8Tests
{
    [Fact]
    public void RefString8_FromSlice_SeesExactlyTheSlice()
    {
        // "abcdef"u8 literal is null-terminated in the data section, so this read is safe to run.
        RefString8 r = "abcdef"u8[..3];

        Assert.Equal(3, r.Length);
        Assert.True("abc"u8.SequenceEqual(r.AsSpan()));
        Assert.Equal("abc", r.ToString16());
        Assert.False(r.IsEmpty);
    }

    [Fact]
    public void RefString8_FromArrayWithoutNul_SeesWholeArray()
    {
        RefString8 r = new byte[] { (byte)'a', (byte)'b', (byte)'c' };

        Assert.Equal(3, r.Length);
        Assert.Equal("abc", r.ToString16());
    }

    [Fact]
    public void RefString8_FromSpanWithEmbeddedNul_StopsAtNul()
    {
        RefString8 r = "a\0bc"u8;

        Assert.Equal(1, r.Length);
        Assert.Equal("a", r.ToString16());
    }

    [Fact]
    public void RefString8_Empty_HasZeroLengthAndIsEmpty()
    {
        var r = RefString8.Empty;

        Assert.Equal(0, r.Length);
        Assert.True(r.IsEmpty);
    }

    [Fact]
    public void RefString8_Default_HasZeroLengthAndIsEmpty()
    {
        RefString8 r = default;

        Assert.Equal(0, r.Length);
        Assert.True(r.IsEmpty);
    }
}
