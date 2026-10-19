using Brutal.Enums;

namespace Brutal.Core.Tests;

public enum SignedRangeEnum
{
    Negative = -1,
    Zero = 0,
    One = 1,
}

public class EnumTests
{
    [Fact]
    public void Enum_MinValue_MaxValue_respect_signed_ordering()
    {
        Assert.Equal(SignedRangeEnum.Negative, Enum<SignedRangeEnum>.MinValue);
        Assert.Equal(SignedRangeEnum.One, Enum<SignedRangeEnum>.MaxValue);
    }

    [Fact]
    public void EnumMask_rejects_negative_enum_with_NotSupportedException()
    {
        var ex = Assert.Throws<TypeInitializationException>(() => EnumMask<SignedRangeEnum>.Empty.ToString());
        Assert.IsType<NotSupportedException>(ex.InnerException);
    }
}
