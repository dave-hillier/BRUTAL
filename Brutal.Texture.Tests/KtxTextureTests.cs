using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Brutal.KtxApi;
using KtxTexture = Brutal.TextureApi.Ktx.KtxTexture;

namespace Brutal.Texture.Tests;

public class KtxTextureTests
{
    [Fact]
    public unsafe void KtxTexture2_row_pitch_rounds_partial_blocks_up()
    {
        // A KTX2 BC1 texture 20 texels wide: level 1 is 10 texels = 3 blocks of 8 bytes
        var native = (byte*)NativeMemory.AllocZeroed(256);
        var prot = (byte*)NativeMemory.AllocZeroed(256);
        try
        {
            *(ClassId*)(native + 0) = ClassId.Texture2C;
            *(KtxTextureProtected**)(native + 24) = (KtxTextureProtected*)prot;
            *(uint*)(native + 36) = 20; // BaseWidth
            *(uint*)(native + 40) = 20; // BaseHeight
            *(uint*)(native + 44) = 1; // BaseDepth
            *(uint*)(native + 52) = 3; // NumLevels
            *(uint*)(prot + 24 + 8) = 64; // FormatSize.BlockSizeInBits
            *(uint*)(prot + 24 + 12) = 4; // FormatSize.BlockWidth
            *(uint*)(prot + 24 + 16) = 4; // FormatSize.BlockHeight

            var texture = CreateWithoutNativeLoad(native);

            Assert.Equal(5 * 8, (int)texture.RowPitch(0));
            Assert.Equal(3 * 8, (int)texture.RowPitch(1));
        }
        finally
        {
            NativeMemory.Free(native);
            NativeMemory.Free(prot);
        }
    }

    [Fact]
    public unsafe void KtxTexture2_row_pitch_counts_a_partial_block_at_level_zero()
    {
        // 6 texels of BC1 need 2 blocks
        var native = (byte*)NativeMemory.AllocZeroed(256);
        var prot = (byte*)NativeMemory.AllocZeroed(256);
        try
        {
            *(ClassId*)(native + 0) = ClassId.Texture2C;
            *(KtxTextureProtected**)(native + 24) = (KtxTextureProtected*)prot;
            *(uint*)(native + 36) = 6; // BaseWidth
            *(uint*)(native + 40) = 6; // BaseHeight
            *(uint*)(native + 44) = 1; // BaseDepth
            *(uint*)(native + 52) = 1; // NumLevels
            *(uint*)(prot + 24 + 8) = 64; // FormatSize.BlockSizeInBits
            *(uint*)(prot + 24 + 12) = 4; // FormatSize.BlockWidth
            *(uint*)(prot + 24 + 16) = 4; // FormatSize.BlockHeight

            var texture = CreateWithoutNativeLoad(native);

            Assert.Equal(2 * 8, (int)texture.RowPitch(0));
        }
        finally
        {
            NativeMemory.Free(native);
            NativeMemory.Free(prot);
        }
    }

    // Bypasses the constructor, which calls into native libktx
    private static unsafe KtxTexture CreateWithoutNativeLoad(byte* native)
    {
        var texture = (KtxTexture)RuntimeHelpers.GetUninitializedObject(typeof(KtxTexture));
        typeof(KtxTexture)
            .GetField("_commonTextureHandle", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(texture, new KtxTextureHandle((nint)native));
        return texture;
    }
}
