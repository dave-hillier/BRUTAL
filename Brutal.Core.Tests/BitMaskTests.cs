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

    [Fact]
    public void Highest_valid_bit_index_is_usable_on_a_byte_mask()
    {
        var mask = BitMask<byte>.FromBit(7);

        Assert.Equal((byte)0x80, mask.Value);
        Assert.True(mask.Contains(7));
        Assert.False(mask.Contains(0));
    }

    [Theory]
    [InlineData(8)]
    [InlineData(255)]
    public void Out_of_range_bit_index_is_rejected_by_every_byte_index_entry_point(byte bitIndex)
    {
        var mask = BitMask<byte>.Empty;

        Assert.Throws<ArgumentOutOfRangeException>(() => BitMask<byte>.FromBit(bitIndex));
        Assert.Throws<ArgumentOutOfRangeException>(() => mask.Contains(bitIndex));
        Assert.Throws<ArgumentOutOfRangeException>(() => mask.Add(bitIndex));
        Assert.Throws<ArgumentOutOfRangeException>(() => mask.Remove(bitIndex));
        Assert.Throws<ArgumentOutOfRangeException>(() => mask.Set(bitIndex, true));
        Assert.Throws<ArgumentOutOfRangeException>(() => mask[bitIndex]);
        Assert.Throws<ArgumentOutOfRangeException>(() => mask[bitIndex] = true);
    }

    [Fact]
    public void Multi_bit_constructor_sets_each_valid_bit_index()
    {
        var mask = new BitMask<byte>(new byte[] { 0, 3, 7 });

        Assert.Equal((byte)0b1000_1001, mask.Value);
    }

    [Fact]
    public void Multi_bit_constructor_rejects_an_out_of_range_bit_index_instead_of_aliasing_bit_zero()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BitMask<byte>(new byte[] { 1, 8 }));
    }

    [Fact]
    public void Index_from_end_addresses_the_highest_bit()
    {
        var mask = BitMask<ulong>.Empty;

        mask[^1] = true;

        Assert.Equal(1UL << 63, mask.Value);
        Assert.True(mask[^1]);
        Assert.True(mask[63]);
        Assert.False(mask[^64]);
    }

    [Fact]
    public void Index_from_end_equal_to_the_bit_count_addresses_bit_zero()
    {
        var mask = BitMask<ulong>.Empty;

        mask[^64] = true;

        Assert.Equal(1UL, mask.Value);
        Assert.True(mask[^64]);
    }

    [Fact]
    public void Index_from_end_past_the_bit_count_is_rejected()
    {
        var mask = BitMask<ulong>.Empty;

        Assert.Throws<ArgumentOutOfRangeException>(() => mask[^65]);
        Assert.Throws<ArgumentOutOfRangeException>(() => mask[^65] = true);
        Assert.Equal(0UL, mask.Value);
    }

    [Fact]
    public void Index_from_end_whose_negative_offset_truncates_to_a_valid_byte_is_rejected()
    {
        // ^264 on a byte mask has offset 8 - 264 = -256, which truncates to bit 0 if cast to byte unchecked.
        var mask = BitMask<byte>.Empty;

        Assert.Throws<ArgumentOutOfRangeException>(() => mask[^264]);
        Assert.Throws<ArgumentOutOfRangeException>(() => mask[^264] = true);
        Assert.Equal((byte)0, mask.Value);
    }

    [Fact]
    public void Index_from_start_at_the_bit_count_is_rejected()
    {
        var mask = BitMask<ulong>.Empty;

        Assert.Throws<ArgumentOutOfRangeException>(() => mask[new Index(64)]);
        Assert.Throws<ArgumentOutOfRangeException>(() => mask[^0] = true);
        Assert.Equal(0UL, mask.Value);
    }
}
