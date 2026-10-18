namespace Brutal.Strings.Tests;

public class Utf8PathTests
{
    private static void AssertInvalidUtf8HandledCorrectly(Func<byte[]> call, byte[] expected)
    {
        byte[]? result = null;
        Exception? ex = null;
        try
        {
            result = call();
        }
        catch (Exception e)
        {
            ex = e;
        }

        var ok = ex?.GetType() == typeof(ArgumentException) || (result is not null && result.AsSpan().SequenceEqual(expected));
        Assert.True(
            ok,
            $"Expected ArgumentException (documented 'Invalid UTF-8 path') or [{Convert.ToHexString(expected)}], "
                + $"got {(ex is null ? "[" + Convert.ToHexString(result!) + "]" : ex.GetType().Name + ": " + ex.Message)}"
        );
    }

    [Fact]
    public void Utf8Path_GetDirectoryName_InvalidUtf8_ThrowsOrReturnsCorrectPrefix()
    {
        byte[] path = [0xFF, (byte)'/', (byte)'a', (byte)'b'];
        AssertInvalidUtf8HandledCorrectly(() => Utf8.Path.GetDirectoryName(path).ToArray(), [0xFF]);
    }

    [Fact]
    public void Utf8Path_GetFileName_InvalidUtf8_ThrowsOrReturnsCorrectSuffix()
    {
        byte[] path = [(byte)'d', (byte)'i', (byte)'r', (byte)'/', 0xFF];
        AssertInvalidUtf8HandledCorrectly(() => Utf8.Path.GetFileName(path).ToArray(), [0xFF]);
    }
}
