using System.Runtime.InteropServices;
using Brutal.Memory;
using Brutal.Pointers;

namespace Brutal.Core.Tests;

public unsafe class ByteSizeTests
{
    [Fact]
    public void ByteSize_Of_T_count_does_not_overflow_int()
    {
        // 300M longs = 2.4 GB, well within nuint on 64-bit
        var size = ByteSize.Of<long>(300_000_000);
        Assert.Equal(2_400_000_000UL, (ulong)size.Bytes);
    }

    [Fact]
    public void ByteSize_Of_T_count_does_not_wrap_to_a_smaller_size()
    {
        // 300M Guids = 4.8 GB; int overflow wraps to a *positive* 505,032,704
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
            int asInt = fiveGb; // implicit
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
            int asInt = threeGb; // implicit
            Assert.Equal(3L << 30, (long)asInt);
        });
        Assert.IsType<OverflowException>(ex);
    }

    [Theory]
    [InlineData(1048575UL)] // 1 MiB - 1
    [InlineData(1099511627775UL)] // 1 TiB - 1
    public void ByteSize_ToString_never_shows_1024_of_a_unit(ulong bytes)
    {
        var s = new ByteSize((nuint)bytes).ToString();
        Assert.DoesNotContain("1024 ", s);
    }

    // Asks the C allocator behind NativeMemory how many bytes a block really holds.
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
