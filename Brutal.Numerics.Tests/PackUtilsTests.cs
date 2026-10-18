namespace Brutal.Numerics.Tests;

public class PackUtilsTests
{
    [Fact]
    public void SByte_ToUInt_Cast_MatchesCSharpCast()
    {
        sbyte v = -1;
        Assert.Equal(unchecked((uint)v), PackUtils.SByte.ToUInt(v, Unpack.IntegerMixed.Cast));
    }
}
