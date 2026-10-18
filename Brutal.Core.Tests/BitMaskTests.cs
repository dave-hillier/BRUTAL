using Brutal.Enums;

namespace Brutal.Core.Tests;

public class BitMaskTests
{
    [Fact]
    public void BitMask_out_of_range_bit_index_does_not_alias_bit_zero()
    {
        var mask = BitMask<byte>.FromBit(0);
        var ex = Record.Exception(() => Assert.False(mask.Contains(8)));
        if (ex is not null)
            Assert.IsType<ArgumentOutOfRangeException>(ex);
    }

    [Fact]
    public void BitMask_hat_zero_index_does_not_set_bit_zero()
    {
        var mask = BitMask<ulong>.Empty;
        var ex = Record.Exception(() =>
        {
            mask[^0] = true;
            Assert.False(mask[0]);
        });
        if (ex is not null)
            Assert.IsType<ArgumentOutOfRangeException>(ex);
    }
}
