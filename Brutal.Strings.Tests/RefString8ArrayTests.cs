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
                Assert.InRange(offset, 0, 7); // must point inside the string-bytes region (do not deref otherwise)
            }

            Assert.Equal("abc", new string((sbyte*)ptrs[0]));
            Assert.Equal("def", new string((sbyte*)ptrs[1]));
        }
    }
}
