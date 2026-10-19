using System.Runtime.InteropServices;
using Brutal.Memory;
using Brutal.Pointers;

namespace Brutal.Core.Tests;

public unsafe class ByteSizeTests
{
    [Fact]
    public void ByteSize_Of_T_count_does_not_overflow_int()
    {
        var size = ByteSize.Of<long>(300_000_000);
        Assert.Equal(2_400_000_000UL, (ulong)size.Bytes);
    }

    [Fact]
    public void ByteSize_Of_T_count_does_not_wrap_to_a_smaller_size()
    {
        // int overflow wraps to a positive 505,032,704
        var size = ByteSize.Of<Guid>(300_000_000);
        Assert.Equal(4_800_000_000UL, (ulong)size.Bytes);
    }

    [Fact]
    public void Mem_Realloc_T_large_count_allocates_requested_bytes()
    {
        var p = Mem.Alloc<long>();
        Ptr<long> grown = default;
        try
        {
            // 2.4 GB of virtual memory: lazily committed on macOS/Linux
            grown = Mem.Realloc(p, 300_000_000);
            Assert.False(grown.IsNull());
        }
        finally
        {
            Mem.Free(grown.IsNull() ? p : grown);
        }
    }

    [Fact]
    public void Mem_Realloc_T_allocates_at_least_count_elements()
    {
        var p = Mem.Alloc<Guid>();
        var grown = Mem.Realloc(p, 300_000_000);
        try
        {
            var usable = UsableSize(grown.Pointer);
            Assert.True(usable >= 4_800_000_000UL, $"allocated only {usable} bytes for 300,000,000 Guids");
        }
        finally
        {
            Mem.Free(grown);
        }
    }

    [Fact]
    public void ByteSize_ToElementCount_never_returns_negative_count()
    {
        var threeGb = new ByteSize((nuint)(3UL << 30));
        var ex = Record.Exception(() => threeGb.ToElementCount<byte>());
        if (ex is null)
            Assert.True(threeGb.ToElementCount<byte>().Value >= 0, $"got {threeGb.ToElementCount<byte>().Value}");
        else
            Assert.IsType<OverflowException>(ex);
    }

    [Fact]
    public void ByteSize64_implicit_int_conversion_does_not_silently_truncate()
    {
        var fiveGb = new ByteSize64(5UL << 30);
        var ex = Record.Exception(() =>
        {
            int asInt = fiveGb;
            Assert.Equal(5L << 30, (long)asInt);
        });
        Assert.IsType<OverflowException>(ex);
    }

    [Fact]
    public void ByteSize32_implicit_int_conversion_does_not_silently_truncate()
    {
        var threeGb = new ByteSize32(3u << 30);
        var ex = Record.Exception(() =>
        {
            int asInt = threeGb;
            Assert.Equal(3L << 30, (long)asInt);
        });
        Assert.IsType<OverflowException>(ex);
    }

    [Theory]
    [InlineData(0UL, "0 B")]
    [InlineData(1023UL, "1023 B")]
    [InlineData(1024UL, "1 KB")]
    [InlineData(1048570UL, "1023.99 KB")] // 1023.994 KB stays in KB
    [InlineData(1048571UL, "1 MB")] // 1023.995 KB
    [InlineData(1048575UL, "1 MB")] // 1 MiB - 1
    [InlineData(1048576UL, "1 MB")]
    [InlineData(1099511627775UL, "1 TB")] // 1 TiB - 1
    [InlineData(ulong.MaxValue, "16 EB")]
    public void ByteSize_ToString_never_shows_1024_of_a_unit(ulong bytes, string expected)
    {
        Assert.Equal(expected, new ByteSize((nuint)bytes).ToString());
    }

    // Exact binary fractions land on the midpoint
    [Theory]
    [InlineData(1152UL, "1.13 KB")] // 1.125
    [InlineData(1664UL, "1.63 KB")] // 1.625
    [InlineData(2176UL, "2.13 KB")] // 2.125
    [InlineData(1179648UL, "1.13 MB")] // 1.125
    public void ByteSize_ToString_rounds_midpoints_away_from_zero(ulong bytes, string expected)
    {
        Assert.Equal(expected, new ByteSize((nuint)bytes).ToString());
        Assert.Equal(expected, new ByteSize64(bytes).ToString());
        Assert.Equal(expected, new ByteSize32((uint)bytes).ToString());
    }

    [Theory]
    [InlineData(1048575U, "1 MB")]
    [InlineData(uint.MaxValue, "4 GB")]
    public void ByteSize32_ToString_promotes_unit_like_ByteSize(uint bytes, string expected)
    {
        Assert.Equal(expected, new ByteSize32(bytes).ToString());
    }

    [Theory]
    [InlineData(1048575UL, "1 MB")]
    [InlineData(1099511627775UL, "1 TB")]
    public void ByteSize64_ToString_promotes_unit_like_ByteSize(ulong bytes, string expected)
    {
        Assert.Equal(expected, new ByteSize64(bytes).ToString());
    }

    // Usable size of a NativeMemory block, from the C allocator
    static nuint UsableSize(void* ptr)
    {
        if (OperatingSystem.IsMacOS())
            return malloc_size(ptr);
        if (OperatingSystem.IsLinux())
            return malloc_usable_size(ptr);
        if (OperatingSystem.IsWindows())
            return _msize(ptr);
        throw new PlatformNotSupportedException();
    }

    [DllImport("libSystem.dylib")]
    static extern nuint malloc_size(void* ptr);

    [DllImport("libc")]
    static extern nuint malloc_usable_size(void* ptr);

    [DllImport("ucrtbase")]
    static extern nuint _msize(void* ptr);
}
