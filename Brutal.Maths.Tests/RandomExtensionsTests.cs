using Brutal.Numerics;

namespace Brutal.Maths.Tests;

public class RandomExtensionsTests
{
    private const int SampleCount = 200_000;

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

    [Theory]
    [InlineData(float.Epsilon)]
    [InlineData(1e-40f)]
    public void NextSingle_Max_IsExclusiveOfMax(float maxValue)
    {
        // a subnormal maxValue lets the product round up to maxValue itself
        var v = new MaxRandom().NextSingle(maxValue);
        Assert.True(v < maxValue, $"returned {v}");
    }

    [Theory]
    [InlineData(double.Epsilon)]
    [InlineData(1e-310)]
    public void NextDouble_Max_IsExclusiveOfMax(double maxValue)
    {
        var v = new MaxRandom().NextDouble(maxValue);
        Assert.True(v < maxValue, $"returned {v}");
    }

    [Fact]
    public void NextSingle_Max_ZeroReturnsZero()
    {
        Assert.Equal(0f, new MaxRandom().NextSingle(0f));
    }

    [Fact]
    public void NextDouble_Max_ZeroReturnsZero()
    {
        Assert.Equal(0.0, new MaxRandom().NextDouble(0.0));
    }

    [Fact]
    public void NextSingleUnit3_ReturnsUnitVectors()
    {
        var r = new Random(12345);
        for (var i = 0; i < SampleCount; i++)
        {
            var v = r.NextSingleUnit3();
            Assert.InRange(v.Length(), 1f - 1e-5f, 1f + 1e-5f);
        }
    }

    [Fact]
    public void NextDoubleUnit3_ReturnsUnitVectors()
    {
        var r = new Random(12345);
        for (var i = 0; i < SampleCount; i++)
        {
            var v = r.NextDoubleUnit3();
            Assert.InRange(v.Length(), 1.0 - 1e-12, 1.0 + 1e-12);
        }
    }

    [Fact]
    public void NextSingleUnit3_IsUniformOnSphere()
    {
        var r = new Random(12345);
        AssertUniformOnSphere(() =>
        {
            var v = r.NextSingleUnit3();
            return (v.X, v.Y, v.Z);
        });
    }

    [Fact]
    public void NextDoubleUnit3_IsUniformOnSphere()
    {
        var r = new Random(12345);
        AssertUniformOnSphere(() =>
        {
            var v = r.NextDoubleUnit3();
            return (v.X, v.Y, v.Z);
        });
    }

    [Fact]
    public void NextSinglePointInSphere_IsUniformInVolume()
    {
        var r = new Random(12345);
        AssertUniformInBall(() => r.NextSinglePointInSphere().Length());
    }

    [Fact]
    public void NextDoublePointInSphere_IsUniformInVolume()
    {
        var r = new Random(12345);
        AssertUniformInBall(() => r.NextDoublePointInSphere().Length());
    }

    // Uniform on the sphere => each coordinate is uniform in [-1, 1] (Archimedes),
    // so P(c > 0.5) = P(c < -0.5) = 0.25 on every axis. Checking both tails of all
    // three axes catches a z confined to one hemisphere and a mis-scaled x or y.
    private static void AssertUniformOnSphere(Func<(double X, double Y, double Z)> sample)
    {
        var above = new int[3];
        var below = new int[3];
        for (var i = 0; i < SampleCount; i++)
        {
            var (x, y, z) = sample();
            double[] coordinates = [x, y, z];
            for (var axis = 0; axis < 3; axis++)
            {
                if (coordinates[axis] > 0.5) above[axis]++;
                if (coordinates[axis] < -0.5) below[axis]++;
            }
        }

        for (var axis = 0; axis < 3; axis++)
        {
            Assert.InRange(above[axis] / (double)SampleCount, 0.24, 0.26);
            Assert.InRange(below[axis] / (double)SampleCount, 0.24, 0.26);
        }
    }

    // Uniform in the unit ball => P(r < 0.5) = 0.5^3 = 0.125 and P(r < 0.8) = 0.8^3 = 0.512
    private static void AssertUniformInBall(Func<double> sampleRadius)
    {
        var insideHalf = 0;
        var insideFourFifths = 0;
        for (var i = 0; i < SampleCount; i++)
        {
            var radius = sampleRadius();
            Assert.True(radius <= 1.0 + 1e-6, $"radius {radius} is outside the unit ball");
            if (radius < 0.5) insideHalf++;
            if (radius < 0.8) insideFourFifths++;
        }

        Assert.InRange(insideHalf / (double)SampleCount, 0.12, 0.13);
        Assert.InRange(insideFourFifths / (double)SampleCount, 0.502, 0.522);
    }
}
