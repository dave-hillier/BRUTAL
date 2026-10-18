namespace Brutal.Strings.Tests;

public class RefString8Tests
{
    [Fact]
    public void RefString8_FromSlice_LengthDoesNotExceedSlice()
    {
        // "abcdef"u8 literal is null-terminated in the data section, so this read is safe to run.
        RefString8 r = "abcdef"u8[..3];
        Assert.True(r.Length <= 3, $"RefString8 built from a 3-byte span reports Length {r.Length}");
    }
}
