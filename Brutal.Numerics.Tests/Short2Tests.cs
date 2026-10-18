namespace Brutal.Numerics.Tests;

public class Short2Tests
{
    [Fact]
    public void Short2_LengthSquared_IsNonNegative()
    {
        var v = new short2(short.MinValue, short.MinValue);
        Assert.True(v.LengthSquared() >= 0, $"LengthSquared was {v.LengthSquared()}");
    }

    [Fact]
    public void Short2_DistanceSquared_IsNonNegative()
    {
        var a = new short2(short.MaxValue, 0);
        var b = new short2(short.MinValue, 0);
        Assert.True(short2.DistanceSquared(a, b) >= 0, $"DistanceSquared was {short2.DistanceSquared(a, b)}");
    }
}
