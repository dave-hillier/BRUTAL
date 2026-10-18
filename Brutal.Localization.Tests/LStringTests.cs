namespace Brutal.Localization.Tests;

public class LStringTests
{
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
