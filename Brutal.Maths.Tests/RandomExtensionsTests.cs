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

    [Fact]
    public void NextSingleUnit3_IsUniformOnSphere()
    {
        // uniform on the sphere => z is uniform in [-1, 1] (Archimedes), so P(|z| > 0.5) = 0.5
        var r = new Random(12345);
        const int n = 200_000;
        var count = 0;
        for (var i = 0; i < n; i++)
            if (MathF.Abs(r.NextSingleUnit3().Z) > 0.5f) count++;
        Assert.InRange(count / (double)n, 0.49, 0.51);
    }

    [Fact]
    public void NextSinglePointInSphere_IsUniformInVolume()
    {
        // uniform in the unit ball => P(r < 0.5) = 0.5^3 = 0.125
        var r = new Random(12345);
        const int n = 200_000;
        var count = 0;
        for (var i = 0; i < n; i++)
            if (r.NextSinglePointInSphere().Length() < 0.5f) count++;
        Assert.InRange(count / (double)n, 0.12, 0.13);
    }
}
