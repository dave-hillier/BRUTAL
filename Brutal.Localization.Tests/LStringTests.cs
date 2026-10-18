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

    [Fact]
    public void RemoveLocalization_restores_template_behaviour_for_repeated_placeholder()
    {
        var s = new LString("pair", "{A} and {A}");
        var before = s.Format("x", "y");

        s.RemoveLocalization();
        var after = s.Format("x", "y");

        Assert.Equal(before, after);
    }
}
