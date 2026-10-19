using System.Buffers;

namespace Brutal.Strings.Tests;

public class RefString8ArrayTests
{
    [Fact]
    public unsafe void RefString8Array_AppendString_ThenCommit_PointersReferToAppendedStrings()
    {
        // room for "abc\0" + "def\0"
        using var array = new RefString8Array(2, (ByteSize)8);
        array.AppendString("abc"u8);
        array.AppendString("def"u8);

        fixed (byte* p = array)
        {
            var ptrs = array.CommitFixedRegion(p);
            for (var i = 0; i < 2; i++)
            {
                var offset = (long)ptrs[i] - (long)p;
                Assert.InRange(offset, 0, 7); // check before dereferencing
            }

            Assert.Equal("abc", new string((sbyte*)ptrs[0]));
            Assert.Equal("def", new string((sbyte*)ptrs[1]));
        }
    }

    [Fact]
    public unsafe void RefString8Array_AppendString_AfterString16SpanConstruction_ThrowsFullAndKeepsOriginalStrings()
    {
        using var array = new RefString8Array((ReadOnlySpan<string>)["abc"]);

        Assert.Equal(1, (int)array.Count);
        InvalidOperationException? ex = null;
        try
        {
            array.AppendString("x"u8);
        }
        catch (InvalidOperationException e)
        {
            ex = e;
        }

        Assert.NotNull(ex);
        Assert.Equal("Array is full", ex.Message);

        fixed (byte* p = array)
        {
            var ptrs = array.CommitFixedRegion(p);
            Assert.Equal("abc", new string((sbyte*)ptrs[0]));
        }
    }

    [Fact]
    public unsafe void RefString8Array_AppendString_AfterString8SpanConstruction_ThrowsFullAndKeepsOriginalStrings()
    {
        using var array = new RefString8Array((ReadOnlySpan<String8>)[new String8("abc"u8), new String8("de"u8)]);

        Assert.Equal(2, (int)array.Count);
        InvalidOperationException? ex = null;
        try
        {
            array.AppendString("x"u8);
        }
        catch (InvalidOperationException e)
        {
            ex = e;
        }

        Assert.NotNull(ex);
        Assert.Equal("Array is full", ex.Message);

        fixed (byte* p = array)
        {
            var ptrs = array.CommitFixedRegion(p);
            Assert.Equal("abc", new string((sbyte*)ptrs[0]));
            Assert.Equal("de", new string((sbyte*)ptrs[1]));
        }
    }

    [Fact]
    public unsafe void RefString8Array_CommitFixedRegion_WhenPartiallyFilled_Throws()
    {
        // Dirty the shared pool so an uninitialised offset would be garbage rather than zero.
        for (var i = 0; i < 8; i++)
        {
            var dirty = ArrayPool<byte>.Shared.Rent(64);
            dirty.AsSpan().Fill(0x7F);
            ArrayPool<byte>.Shared.Return(dirty);
        }

        using var array = new RefString8Array(3, (ByteSize)16);
        array.AppendString("abc"u8);
        array.AppendString("def"u8);

        fixed (byte* p = array)
        {
            InvalidOperationException? ex = null;
            try
            {
                array.CommitFixedRegion(p);
            }
            catch (InvalidOperationException e)
            {
                ex = e;
            }

            Assert.NotNull(ex);
            Assert.Equal("Only 2 of 3 strings have been appended", ex.Message);
        }
    }
}
