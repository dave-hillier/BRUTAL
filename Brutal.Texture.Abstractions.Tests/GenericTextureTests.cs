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
}
