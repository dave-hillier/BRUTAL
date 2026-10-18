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
}
