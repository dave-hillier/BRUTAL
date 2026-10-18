namespace Brutal.Maths.Tests;

public class RandomExtensionsTests
{
    private sealed class MaxRandom : Random
    {
        public override float NextSingle() => MathF.BitDecrement(1f); // largest value Random.NextSingle returns
        public override double NextDouble() => Math.BitDecrement(1.0); // largest value Random.NextDouble returns
    }

    [Fact]
    public void NextSingle_MinMax_IsExclusiveOfMax()
    {
        var v = new MaxRandom().NextSingle(1f, 3f);
        Assert.True(v < 3f, $"returned {v}");
    }

    [Fact]
    public void NextDouble_MinMax_IsExclusiveOfMax()
    {
        var v = new MaxRandom().NextDouble(1.0, 3.0);
        Assert.True(v < 3.0, $"returned {v}");
    }
}
