namespace Brutal.Collections.Tests;

public class NameIdentityTests
{
    private sealed class Tag;

    [Fact]
    public void SameString_StillEqual_AfterManyOtherNamesAreCreated()
    {
        var pool = new Namespace<Tag>();
        var first = new Name<Tag>("player", pool);

        for (var i = 0; i < 100_000; i++)
            _ = new Name<Tag>($"entity-{i}", pool);

        var second = new Name<Tag>("player", pool);

        Assert.Equal(first, second);
        Assert.True(pool.Contains("player"));
    }
}
