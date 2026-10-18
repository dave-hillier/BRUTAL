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
