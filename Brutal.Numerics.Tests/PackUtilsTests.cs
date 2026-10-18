namespace Brutal.Numerics.Tests;

public class PackUtilsTests
{
    [Fact]
    public void SByte_Normalize_RoundTrip_Negative()
    {
        sbyte v = -64;
        var f = PackUtils.SByte.ToFloat(v, Unpack.Float.Normalize); // -0.5
        Assert.Equal(v, PackUtils.Float.ToSByte(f, Pack.Float.Normalize));
    }

    [Fact]
    public void Float_And_Double_Normalize_Agree()
    {
        Assert.Equal(PackUtils.Float.ToByte(0.5f, Pack.Float.Normalize), PackUtils.Double.ToByte(0.5, Pack.Float.Normalize));
    }
}
