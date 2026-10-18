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

    [Theory]
    // Right placeholder count, unknown name: throws inside the build loop after earlier entries were built.
    [InlineData(null, "Yo {C} {X}", "Hi {N} {C}", "Hi b 3")]
    [InlineData("Yo {C} {N}", "Yo {N} {X}", "Yo {C} {N}", "Yo 3 b")]
    // Too many placeholders: throws inside the build loop on the extra occurrence.
    [InlineData(null, "Yo {C} {N} {N}", "Hi {N} {C}", "Hi b 3")]
    [InlineData("Yo {C} {N}", "Yo {N} {C} {X}", "Yo {C} {N}", "Yo 3 b")]
    public void Localize_rejected_mid_build_leaves_LString_unchanged(
        string? previousLocalization,
        string rejectedLocalization,
        string expectedLocalized,
        string expectedFormatted
    )
    {
        var s = new LString("pair", "Hi {N} {C}");
        if (previousLocalization is not null)
            s.Localize(previousLocalization);

        Assert.Throws<ArgumentException>(() => s.Localize(rejectedLocalization));

        Assert.Equal(expectedLocalized, s.Localized);
        Assert.Equal(expectedFormatted, s.Format("b", 3));
    }

    [Fact]
    public void RemoveLocalization_restores_template_behaviour_for_repeated_placeholder()
    {
        var s = new LString("pair", "{A} and {A}");
        var before = s.Format("x", "y");

        s.RemoveLocalization();
        var after = s.Format("x", "y");

        Assert.Equal("x and y", before);
        Assert.Equal(before, after);
    }

    [Fact]
    public void Localize_maps_repeated_placeholder_occurrences_in_order()
    {
        var s = new LString("pair", "{A} and {A}");

        s.Localize("{A} et {A}");

        Assert.Equal("x et y", s.Format("x", "y"));
    }

    [Fact]
    public void Localize_rejects_more_occurrences_of_a_placeholder_than_the_template_has()
    {
        var s = new LString("pair", "{A} and {B}");

        Assert.Throws<ArgumentException>(() => s.Localize("{A} et {A}"));

        Assert.Equal("{A} and {B}", s.Localized);
        Assert.Equal("x and y", s.Format("x", "y"));
    }
}
