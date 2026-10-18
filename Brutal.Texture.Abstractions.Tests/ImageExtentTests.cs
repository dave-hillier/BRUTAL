using Brutal.Numerics;
using Brutal.TextureApi;
using Brutal.TextureApi.Abstractions;

namespace Brutal.Texture.Abstractions.Tests;

public class ImageExtentTests
{
    [Fact]
    public void ImageExtent_texels_are_clamped_to_one_like_the_blocks()
    {
        var extent = new ImageExtent(new TexelCount3(8, 8, 1), 1, new TexelCount3(1, 1, 1));
        Assert.Equal(new TexelCount3(4, 4, 1), extent.Texels);
    }

    [Fact]
    public void UploadRegions_stages_every_mip_level_of_a_3d_texture_shallower_than_it_is_wide()
    {
        // 8x8x2 RGBA8 with a full chain: 8x8x2, 4x4x1, 2x2x1, 1x1x1
        var extents = new[] { (8, 8, 2), (4, 4, 1), (2, 2, 1), (1, 1, 1) };
        var info = new GenericTexture.CreateInfo
        {
            Format = TextureFormat.R8G8B8A8_UNorm,
            Extent = new TexelCount3(8, 8, 2),
            LayerCount = 1,
            LevelCount = extents.Length,
            FaceCount = 1,
            IsCubemap = false,
            IsArray = false,
            DimensionCount = 3,
        };

        // ITexture.ImageData returns one depth slice for 3D textures; slices are contiguous
        var ranges = new GenericTexture.CreateImageRange[extents.Length];
        var offset = 0;
        var total = 0;
        for (var i = 0; i < extents.Length; i++)
        {
            var (x, y, z) = extents[i];
            var slice = x * y * 4;
            ranges[i] = new GenericTexture.CreateImageRange
            {
                StartByteOffset = ByteSize.Of(offset),
                ByteLength = ByteSize.Of(slice),
                RowPitch = ByteSize.Of(x * 4),
            };
            offset += slice * z;
            total += slice * z;
        }

        var texture = new GenericTexture(info, ByteSize.Of(total), ranges);
        try
        {
            var regions = UploadRegions.Build(texture);
            var staged = regions.StageBufferRegions.Sum(r => (int)r.SizeInBytes);
            Assert.Equal(total, staged);
        }
        finally
        {
            texture.Destroy();
        }
    }
}
