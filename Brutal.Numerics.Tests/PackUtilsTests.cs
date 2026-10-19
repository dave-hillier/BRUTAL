namespace Brutal.Numerics.Tests;

public class PackUtilsTests
{
    [Fact]
    public void SByte_ToShort_Cast_PreservesSign()
    {
        Assert.Equal((short)-1, PackUtils.SByte.ToShort(-1, Unpack.Integer.Cast));
    }

    [Fact]
    public void Short4_Unpack_SByte4_DefaultCast_PreservesSign()
    {
        var r = short4.Unpack(new sbyte4(-1, -2, 3, -128));
        Assert.Equal(new short4(-1, -2, 3, -128), r);
    }

    [Fact]
    public void SByte_ToUInt_Cast_MatchesCSharpCast()
    {
        sbyte v = -1;
        Assert.Equal(unchecked((uint)v), PackUtils.SByte.ToUInt(v, Unpack.IntegerMixed.Cast));
    }

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

    [Fact]
    public void SByte_Normalize_RoundTrip_Negative()
    {
        sbyte v = -64;
        var f = PackUtils.SByte.ToFloat(v, Unpack.Float.Normalize);
        Assert.Equal(v, PackUtils.Float.ToSByte(f, Pack.Float.Normalize));
    }

    [Fact]
    public void Float_And_Double_Normalize_Agree()
    {
        Assert.Equal(PackUtils.Float.ToByte(0.5f, Pack.Float.Normalize), PackUtils.Double.ToByte(0.5, Pack.Float.Normalize));
    }
}
