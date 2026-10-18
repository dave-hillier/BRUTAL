namespace Brutal.Numerics.Tests;

public class UShort2Tests
{
    [Fact]
    public void UShort2_LengthSquared_IsNonNegative()
    {
        var v = new ushort2(ushort.MaxValue, 0);
        Assert.True(v.LengthSquared() >= 0, $"LengthSquared was {v.LengthSquared()}");
    }
}
