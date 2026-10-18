namespace Brutal.Maths.Tests;

public class RandomExtensionsTests
{
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
