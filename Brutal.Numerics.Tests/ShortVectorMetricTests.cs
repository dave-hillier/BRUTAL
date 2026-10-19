namespace Brutal.Numerics.Tests;

// Each theory puts the extreme value in every component position, so every term must be widened.
public class ShortVectorMetricTests
{
    // 65535 * 65535: one ushort square, or a short difference (32767 - -32768) squared.
    private const long MaxSquare = 4_294_836_225L;

    // -32768 * -32768
    private const long MinShortSquare = 1_073_741_824L;

    // -32768 * 32767
    private const long MinTimesMaxShort = -1_073_709_056L;

    private static short[] ShortsWith(int length, int index, short value)
    {
        var components = new short[length];
        components[index] = value;
        return components;
    }

    private static ushort[] UShortsWith(int length, int index, ushort value)
    {
        var components = new ushort[length];
        components[index] = value;
        return components;
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Short2_metrics_are_exact_with_extreme_in_component(int index)
    {
        var min = new short2(ShortsWith(2, index, short.MinValue));
        var max = new short2(ShortsWith(2, index, short.MaxValue));

        Assert.Equal(MinShortSquare, min.LengthSquared());
        Assert.Equal(MinTimesMaxShort, short2.Dot(min, max));
        Assert.Equal(MaxSquare, short2.DistanceSquared(max, min));
        Assert.Equal(MaxSquare, short2.DistanceSquared(min, max));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Short3_metrics_are_exact_with_extreme_in_component(int index)
    {
        var min = new short3(ShortsWith(3, index, short.MinValue));
        var max = new short3(ShortsWith(3, index, short.MaxValue));

        Assert.Equal(MinShortSquare, min.LengthSquared());
        Assert.Equal(MinTimesMaxShort, short3.Dot(min, max));
        Assert.Equal(MaxSquare, short3.DistanceSquared(max, min));
        Assert.Equal(MaxSquare, short3.DistanceSquared(min, max));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Short4_metrics_are_exact_with_extreme_in_component(int index)
    {
        var min = new short4(ShortsWith(4, index, short.MinValue));
        var max = new short4(ShortsWith(4, index, short.MaxValue));

        Assert.Equal(MinShortSquare, min.LengthSquared());
        Assert.Equal(MinTimesMaxShort, short4.Dot(min, max));
        Assert.Equal(MaxSquare, short4.DistanceSquared(max, min));
        Assert.Equal(MaxSquare, short4.DistanceSquared(min, max));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void UShort2_metrics_are_exact_with_extreme_in_component(int index)
    {
        var max = new ushort2(UShortsWith(2, index, ushort.MaxValue));

        Assert.Equal(MaxSquare, max.LengthSquared());
        Assert.Equal(MaxSquare, ushort2.Dot(max, max));
        Assert.Equal(MaxSquare, ushort2.DistanceSquared(max, default));
        Assert.Equal(MaxSquare, ushort2.DistanceSquared(default, max));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void UShort3_metrics_are_exact_with_extreme_in_component(int index)
    {
        var max = new ushort3(UShortsWith(3, index, ushort.MaxValue));

        Assert.Equal(MaxSquare, max.LengthSquared());
        Assert.Equal(MaxSquare, ushort3.Dot(max, max));
        Assert.Equal(MaxSquare, ushort3.DistanceSquared(max, default));
        Assert.Equal(MaxSquare, ushort3.DistanceSquared(default, max));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void UShort4_metrics_are_exact_with_extreme_in_component(int index)
    {
        var max = new ushort4(UShortsWith(4, index, ushort.MaxValue));

        Assert.Equal(MaxSquare, max.LengthSquared());
        Assert.Equal(MaxSquare, ushort4.Dot(max, max));
        Assert.Equal(MaxSquare, ushort4.DistanceSquared(max, default));
        Assert.Equal(MaxSquare, ushort4.DistanceSquared(default, max));
    }

    // Sums of several extreme squares overflow int even when no single term does.
    [Fact]
    public void Metrics_are_exact_when_every_component_is_extreme()
    {
        Assert.Equal(2 * MinShortSquare, new short2(short.MinValue).LengthSquared());
        Assert.Equal(3 * MinShortSquare, new short3(short.MinValue).LengthSquared());
        Assert.Equal(4 * MinShortSquare, new short4(short.MinValue).LengthSquared());

        Assert.Equal(2 * MaxSquare, short2.DistanceSquared(new short2(short.MaxValue), new short2(short.MinValue)));
        Assert.Equal(3 * MaxSquare, short3.DistanceSquared(new short3(short.MaxValue), new short3(short.MinValue)));
        Assert.Equal(4 * MaxSquare, short4.DistanceSquared(new short4(short.MaxValue), new short4(short.MinValue)));

        Assert.Equal(2 * MaxSquare, new ushort2(ushort.MaxValue).LengthSquared());
        Assert.Equal(3 * MaxSquare, new ushort3(ushort.MaxValue).LengthSquared());
        Assert.Equal(4 * MaxSquare, new ushort4(ushort.MaxValue).LengthSquared());

        Assert.Equal(4 * MaxSquare, ushort4.DistanceSquared(new ushort4(ushort.MaxValue), default));
    }
}
