namespace Brutal.Collections.Tests;

public class NamespaceTests
{
    private sealed class Tag;

    [Fact]
    public void TryGet_OnCustomPool_ReturnsNameEqualToOneCreatedFromThatPool()
    {
        var pool = new Namespace<Tag>();
        var created = new Name<Tag>("custom-pool-entry", pool);

        Assert.True(pool.TryGet("custom-pool-entry", out var found));
        Assert.Equal(created, found!.Value);
    }

    [Fact]
    public void TryGet_OnCustomPool_DoesNotAddToSharedPool()
    {
        var pool = new Namespace<Tag>();
        _ = new Name<Tag>("only-in-custom-pool", pool);

        pool.TryGet("only-in-custom-pool", out _);

        Assert.False(Namespace<Tag>.Shared.Contains("only-in-custom-pool"));
    }
}
