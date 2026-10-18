namespace Brutal.Numerics.Tests;

// Normalize follows the glTF / Vulkan SNORM and UNORM rules:
//   signed unpack:   max(c / Max, -1)          signed pack:   round(clamp(x, -1, 1) * Max)
//   unsigned unpack: c / Max                   unsigned pack: round(clamp(x,  0, 1) * Max)
// so Pack(Unpack(c)) == c for every c except signed MinValue, which shares -1.0 with -Max.
public class PackUtilsRoundTripTests
{
    private const Pack.Float P = Pack.Float.Normalize;
    private const Unpack.Float U = Unpack.Float.Normalize;

    public record Codec(
        string Name,
        long Min,
        long Max,
        bool Exhaustive,
        Func<long, double> Unpack,
        Func<double, long> Pack)
    {
        public override string ToString()
        {
            return Name;
        }
    }

    public static TheoryData<Codec> Codecs()
    {
        return new TheoryData<Codec>
        {
            new("sbyte<->float", sbyte.MinValue, sbyte.MaxValue, true,
                c => PackUtils.SByte.ToFloat((sbyte)c, U), x => PackUtils.Float.ToSByte((float)x, P)),
            new("sbyte<->double", sbyte.MinValue, sbyte.MaxValue, true,
                c => PackUtils.SByte.ToDouble((sbyte)c, U), x => PackUtils.Double.ToSByte(x, P)),
            new("byte<->float", byte.MinValue, byte.MaxValue, true,
                c => PackUtils.Byte.ToFloat((byte)c, U), x => PackUtils.Float.ToByte((float)x, P)),
            new("byte<->double", byte.MinValue, byte.MaxValue, true,
                c => PackUtils.Byte.ToDouble((byte)c, U), x => PackUtils.Double.ToByte(x, P)),
            new("short<->float", short.MinValue, short.MaxValue, true,
                c => PackUtils.Short.ToFloat((short)c, U), x => PackUtils.Float.ToShort((float)x, P)),
            new("short<->double", short.MinValue, short.MaxValue, true,
                c => PackUtils.Short.ToDouble((short)c, U), x => PackUtils.Double.ToShort(x, P)),
            new("ushort<->float", ushort.MinValue, ushort.MaxValue, true,
                c => PackUtils.UShort.ToFloat((ushort)c, U), x => PackUtils.Float.ToUShort((float)x, P)),
            new("ushort<->double", ushort.MinValue, ushort.MaxValue, true,
                c => PackUtils.UShort.ToDouble((ushort)c, U), x => PackUtils.Double.ToUShort(x, P)),
            new("int<->float", int.MinValue, int.MaxValue, false,
                c => PackUtils.Int.ToFloat((int)c, U), x => PackUtils.Float.ToInt((float)x, P)),
            new("int<->double", int.MinValue, int.MaxValue, false,
                c => PackUtils.Int.ToDouble((int)c, U), x => PackUtils.Double.ToInt(x, P)),
            new("uint<->float", uint.MinValue, uint.MaxValue, false,
                c => PackUtils.UInt.ToFloat((uint)c, U), x => PackUtils.Float.ToUInt((float)x, P)),
            new("uint<->double", uint.MinValue, uint.MaxValue, false,
                c => PackUtils.UInt.ToDouble((uint)c, U), x => PackUtils.Double.ToUInt(x, P)),
        };
    }

    private static bool IsSigned(Codec codec)
    {
        return codec.Min < 0;
    }

    private static bool IsFloat(Codec codec)
    {
        return codec.Name.EndsWith("float");
    }

    // float carries 24 significant bits, so 32-bit integers only survive a float round trip up to 2^23.
    private static bool FitsInFloat(long c)
    {
        return Math.Abs(c) <= 1 << 23;
    }

    private static IEnumerable<long> Samples(Codec codec)
    {
        if (codec.Exhaustive)
        {
            for (var c = codec.Min + (IsSigned(codec) ? 1 : 0); c <= codec.Max; c++)
                yield return c;
            yield break;
        }

        long[] edges = [0, 1, -1, 2, -2, 1000, -1000, 12345, -12345, 1 << 23, -(1 << 23)];
        foreach (var e in edges)
            yield return e;
        for (long d = 0; d < 4; d++)
        {
            yield return codec.Max - d;
            if (IsSigned(codec))
                yield return -codec.Max + d;
        }

        var random = new Random(1);
        for (var n = 0; n < 20_000; n++)
            yield return random.NextInt64(IsSigned(codec) ? -codec.Max : 0, codec.Max + 1);
    }

    [Theory]
    [MemberData(nameof(Codecs))]
    public void PackOfUnpack_IsIdentity(Codec codec)
    {
        var failures = Samples(codec)
            .Where(c => c >= codec.Min && c <= codec.Max)
            .Where(c => !IsFloat(codec) || codec.Exhaustive || FitsInFloat(c) || c == codec.Max || c == -codec.Max)
            .Where(c => codec.Pack(codec.Unpack(c)) != c)
            .Take(5)
            .Select(c => $"{c} -> {codec.Unpack(c):R} -> {codec.Pack(codec.Unpack(c))}")
            .ToList();
        Assert.Empty(failures);
    }

    [Theory]
    [MemberData(nameof(Codecs))]
    public void UnpackOfPack_IsIdentity_OnRepresentableValues(Codec codec)
    {
        var failures = Samples(codec)
            .Where(c => c >= codec.Min && c <= codec.Max)
            .Select(codec.Unpack)
            .Where(x => codec.Unpack(codec.Pack(x)) != x)
            .Take(5)
            .Select(x => $"{x:R} -> {codec.Pack(x)} -> {codec.Unpack(codec.Pack(x)):R}")
            .ToList();
        Assert.Empty(failures);
    }

    [Theory]
    [MemberData(nameof(Codecs))]
    public void Endpoints_MapToUnitRange(Codec codec)
    {
        Assert.Equal(0.0, codec.Unpack(0));
        Assert.Equal(1.0, codec.Unpack(codec.Max));
        Assert.Equal(0, codec.Pack(0.0));
        Assert.Equal(codec.Max, codec.Pack(1.0));
        Assert.Equal(codec.Max, codec.Pack(2.0));
        if (IsSigned(codec))
        {
            Assert.Equal(-1.0, codec.Unpack(-codec.Max));
            Assert.Equal(-codec.Max, codec.Pack(-1.0));
            Assert.Equal(-codec.Max, codec.Pack(-2.0));
        }
        else
        {
            Assert.Equal(0, codec.Pack(-1.0));
        }
    }

    [Theory]
    [MemberData(nameof(Codecs))]
    public void SignedMinValue_UnpacksToMinusOne_AndPacksBackToMinusMax(Codec codec)
    {
        if (!IsSigned(codec))
            return;
        Assert.Equal(-1.0, codec.Unpack(codec.Min));
        Assert.Equal(-codec.Max, codec.Pack(codec.Unpack(codec.Min)));
    }

    [Fact]
    public void Int2_Double2_Normalize_RoundTrip_KeepsNegatives()
    {
        var v = new int2(-1, -1000);
        Assert.Equal(v, int2.Pack(double2.Unpack(v, U), P));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MaxValue - 1)]
    [InlineData(int.MaxValue - 2)]
    [InlineData(-int.MaxValue)]
    [InlineData(-int.MaxValue + 1)]
    public void Int_Double_Normalize_RoundTrip(int i)
    {
        Assert.Equal(i, PackUtils.Double.ToInt(PackUtils.Int.ToDouble(i, U), P));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(int.MaxValue)]
    [InlineData(-int.MaxValue)]
    [InlineData(1 << 23)]
    [InlineData(-(1 << 23))]
    public void Int_Float_Normalize_RoundTrip(int i)
    {
        Assert.Equal(i, PackUtils.Float.ToInt(PackUtils.Int.ToFloat(i, U), P));
    }

    [Fact]
    public void Int_MinValue_Normalize_IsMinusOne()
    {
        Assert.Equal(-1.0, PackUtils.Int.ToDouble(int.MinValue, U));
        Assert.Equal(-1f, PackUtils.Int.ToFloat(int.MinValue, U));
        Assert.Equal(-int.MaxValue, PackUtils.Double.ToInt(-1.0, P));
        Assert.Equal(-int.MaxValue, PackUtils.Float.ToInt(-1f, P));
    }
}
