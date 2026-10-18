using Brutal.Numerics;
using Brutal.TextureApi;
using Brutal.TextureApi.Abstractions;

namespace Brutal.Texture.Abstractions.Tests;

public class GenericTextureTests
{
    [Fact]
    public void Defaults_Uncompressed_creates_the_requested_number_of_levels()
    {
        var texture = GenericTexture.Defaults.Uncompressed(TextureFormat.R8G8B8A8_UNorm, new int2(8, 8), levels: 3);
        try
        {
            Assert.Equal(3, texture.LevelCount);
        }
        finally
        {
            texture.Destroy();
        }
    }

    [Fact]
    public void Defaults_Uncompressed_halves_each_level_and_packs_them_contiguously()
    {
        // 8x8, 4x4, 2x2 RGBA8: 256 + 64 + 16 bytes
        var texture = GenericTexture.Defaults.Uncompressed(TextureFormat.R8G8B8A8_UNorm, new int2(8, 8), levels: 3);
        try
        {
            Assert.Equal([32, 16, 8], [(int)texture.RowPitch(0), (int)texture.RowPitch(1), (int)texture.RowPitch(2)]);
            Assert.Equal([256, 64, 16], [Length(texture, 0, 0, 0), Length(texture, 1, 0, 0), Length(texture, 2, 0, 0)]);
            Assert.Equal([0, 256, 320], [Offset(texture, 0, 0, 0), Offset(texture, 1, 0, 0), Offset(texture, 2, 0, 0)]);
            Assert.Equal(256 + 64 + 16, texture.Data.Length);
        }
        finally
        {
            texture.Destroy();
        }
    }

    [Fact]
    public void Defaults_Uncompressed_clamps_short_axes_to_one_texel_on_lower_levels()
    {
        // 8x2, 4x1, 2x1, 1x1 RGBA8
        var texture = GenericTexture.Defaults.Uncompressed(TextureFormat.R8G8B8A8_UNorm, new int2(8, 2), levels: 4);
        try
        {
            Assert.Equal(
                [32, 16, 8, 4],
                [(int)texture.RowPitch(0), (int)texture.RowPitch(1), (int)texture.RowPitch(2), (int)texture.RowPitch(3)]
            );
            Assert.Equal(
                [64, 16, 8, 4],
                [Length(texture, 0, 0, 0), Length(texture, 1, 0, 0), Length(texture, 2, 0, 0), Length(texture, 3, 0, 0)]
            );
            Assert.Equal(
                [0, 64, 80, 88],
                [Offset(texture, 0, 0, 0), Offset(texture, 1, 0, 0), Offset(texture, 2, 0, 0), Offset(texture, 3, 0, 0)]
            );
            Assert.Equal(64 + 16 + 8 + 4, texture.Data.Length);
        }
        finally
        {
            texture.Destroy();
        }
    }

    [Fact]
    public void Defaults_Uncompressed_lays_out_a_cubemap_array_in_level_layer_face_order()
    {
        // 4x4 then 2x2 RGBA8, 2 layers of 6 faces each: 64-byte images then 16-byte images
        var texture = GenericTexture.Defaults.Uncompressed(
            TextureFormat.R8G8B8A8_UNorm,
            new int2(4, 4),
            levels: 2,
            layers: 2,
            faces: 6
        );
        try
        {
            Assert.Equal(2, texture.LevelCount);
            Assert.Equal(2, texture.LayerCount);
            Assert.Equal(6, texture.FaceCount);
            Assert.True(texture.IsCubemap);
            Assert.True(texture.IsArray);
            Assert.Equal(16, (int)texture.RowPitch(0));
            Assert.Equal(8, (int)texture.RowPitch(1));

            for (var layer = 0; layer < 2; ++layer)
            for (var face = 0; face < 6; ++face)
            {
                var image = layer * 6 + face;
                Assert.Equal(64, Length(texture, 0, layer, face));
                Assert.Equal(image * 64, Offset(texture, 0, layer, face));
                Assert.Equal(16, Length(texture, 1, layer, face));
                Assert.Equal(12 * 64 + image * 16, Offset(texture, 1, layer, face));
            }

            Assert.Equal(12 * 64 + 12 * 16, texture.Data.Length);
        }
        finally
        {
            texture.Destroy();
        }
    }

    [Fact]
    public void Defaults_Uncompressed_row_pitch_is_one_row_not_the_whole_image()
    {
        var texture = GenericTexture.Defaults.Uncompressed(TextureFormat.R8G8B8A8_UNorm, new int2(4, 4));
        try
        {
            // 4 texels * 4 bytes, no padding per the GenericTexture docs
            Assert.Equal(16, (int)texture.RowPitch(0));
            Assert.False(new TextureMemoryLayout(texture).ImageLayout(0).IsRowPadded);
        }
        finally
        {
            texture.Destroy();
        }
    }

    private static int Length(GenericTexture texture, int level, int layer, int face) =>
        texture.ImageData(level, layer, face).Length;

    private static int Offset(GenericTexture texture, int level, int layer, int face)
    {
        Assert.True(texture.Data.Overlaps(texture.ImageData(level, layer, face), out var offset));
        return offset;
    }
}
