namespace Brutal.Localization.Tests;

public class LStringTests
{
    [Fact]
    public void Failed_Localize_leaves_LString_unchanged()
    {
        var s = new LString("greeting", "Hello {Name}, you have {Count} items");

        Assert.Throws<ArgumentException>(() => s.Localize("Bonjour {Count}"));

        Assert.Equal("Hello {Name}, you have {Count} items", s.Localized);
        Assert.Equal("Hello Bob, you have 3 items", s.Format("Bob", 3));
    }
}
