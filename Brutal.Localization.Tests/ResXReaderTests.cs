using System.Resources;
using Brutal.Localization.ResX;

namespace Brutal.Localization.Tests;

public class ResXReaderTests
{
    [Fact]
    public void ResXReader_as_IResourceReader_yields_string_values()
    {
        const string resx = """
            <?xml version="1.0" encoding="utf-8"?>
            <root>
              <data name="Hello" xml:space="preserve"><value>World</value></data>
            </root>
            """;
        using var set = new ResourceSet(new ResXReader(new StringReader(resx)));

        Assert.Equal("World", set.GetString("Hello"));
    }
}
