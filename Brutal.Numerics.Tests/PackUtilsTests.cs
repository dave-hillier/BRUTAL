namespace Brutal.Numerics.Tests;

public class PackUtilsTests
{
    [Fact]
    public void UInt_ToFloat_Normalize_MaxIsOne()
    {
        Assert.Equal(1f, PackUtils.UInt.ToFloat(uint.MaxValue, Unpack.Float.Normalize));
    }

    [Fact]
    public void UInt_ToDouble_Normalize_MaxIsOne()
    {
        Assert.Equal(1.0, PackUtils.UInt.ToDouble(uint.MaxValue, Unpack.Float.Normalize));
    }
}
