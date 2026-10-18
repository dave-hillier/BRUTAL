using Brutal.Localization.FixedBuffers;

namespace Brutal.Localization.Tests;

public class LStringTests
{
    [Fact]
    public void Format_into_RingBufferWriter_advances_the_callers_writer()
    {
        var storage = new char[1024];
        var writer = new RingBufferWriter<char>(storage);
        var s = new LString("n", "N{X}");

        var first = s.Format(writer, 1);
        var firstText = first.ToString();
        var second = s.Format(writer, 2);

        Assert.Equal("N1", firstText);
        Assert.Equal("N2", second.ToString());
        // The first result must not have been overwritten by the second Format call.
        Assert.Equal("N1", first.ToString());
    }
}
