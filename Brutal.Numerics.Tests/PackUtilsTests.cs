namespace Brutal.Numerics.Tests;

public class PackUtilsTests
{
    [Fact]
    public void Short_ToFloat_Normalize_MinIsMinusOne()
    {
        Assert.Equal(-1f, PackUtils.SByte.ToFloat(sbyte.MinValue, Unpack.Float.Normalize));
        Assert.True(PackUtils.Short.ToFloat(short.MinValue, Unpack.Float.Normalize) >= -1f,
            $"got {PackUtils.Short.ToFloat(short.MinValue, Unpack.Float.Normalize)}");
    }

    [Fact]
    public void Short_ToDouble_Normalize_MinIsMinusOne()
    {
        Assert.Equal(-1.0, PackUtils.SByte.ToDouble(sbyte.MinValue, Unpack.Float.Normalize));
        Assert.True(PackUtils.Short.ToDouble(short.MinValue, Unpack.Float.Normalize) >= -1.0,
            $"got {PackUtils.Short.ToDouble(short.MinValue, Unpack.Float.Normalize)}");
    }
}
