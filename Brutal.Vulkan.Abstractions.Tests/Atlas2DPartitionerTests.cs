using Brutal.VulkanApi;
using Brutal.VulkanApi.Abstractions;

namespace Brutal.Vulkan.Abstractions.Tests;

public class Atlas2DPartitionerTests
{
    private static ImageViewPartitionInfo FullImagePartition(int width, int height)
    {
        var image = new ImageEx(
            new ImageAllocationInfo
            {
                Extent = new VkExtent3D { Width = width, Height = height, Depth = 1 },
                MipLevels = 1,
                ArrayLayers = 1,
            }
        );
        var view = new ImageViewEx(
            null!,
            image,
            default,
            VkImageViewType._2D,
            new VkImageSubresourceRange { LevelCount = 1, LayerCount = 1 },
            null
        );
        return new ImageViewPartitionInfo(view, 1, 0, image.Extent, new VkOffset3D(), null);
    }

    private static bool Inside(in ImageViewPartitionInfo part, in ImageViewPartitionInfo backing) =>
        part.Offset.X >= backing.Offset.X
        && part.Offset.Y >= backing.Offset.Y
        && part.Offset.X + part.Extent.Width <= backing.Offset.X + backing.Extent.Width
        && part.Offset.Y + part.Extent.Height <= backing.Offset.Y + backing.Extent.Height;

    [Fact]
    public void Atlas2DPartitioner_over_a_sub_partition_stays_inside_it()
    {
        // A 256x256 region at (256, 0) of a 512x512 image, e.g. a slot handed out by a QuadTreePartitioner
        var full = FullImagePartition(512, 512);
        var backing = full with
        {
            Extent = new VkExtent3D { Width = 256, Height = 256, Depth = 1 },
            Offset = new VkOffset3D { X = 256, Y = 0, Z = 0 },
        };
        var atlas = new Atlas2DPartitioner(backing);

        var created = new List<ImageViewPartitionInfo>();
        var request = new ImageViewPartitionInfo.CreateInfo(new VkExtent3D { Width = 128, Height = 128, Depth = 1 }, 1);
        while (created.Count < 64 && atlas.TryCreatePartition(request, out var part))
            created.Add(part);

        Assert.Equal(4, created.Count);
        Assert.All(created, p => Assert.True(Inside(p, backing), $"partition at ({p.Offset.X},{p.Offset.Y}) is outside the backing region"));
    }

    [Fact]
    public void Atlas2DPartitioner_on_a_non_square_image_keeps_rows_inside_its_height()
    {
        var backing = FullImagePartition(256, 64);
        var atlas = new Atlas2DPartitioner(backing);

        var created = new List<ImageViewPartitionInfo>();
        var request = new ImageViewPartitionInfo.CreateInfo(new VkExtent3D { Width = 64, Height = 64, Depth = 1 }, 1);
        while (created.Count < 64 && atlas.TryCreatePartition(request, out var part))
            created.Add(part);

        Assert.All(created, p => Assert.True(Inside(p, backing), $"partition at ({p.Offset.X},{p.Offset.Y}) is outside the 256x64 image"));
        Assert.Equal(4, created.Count);
    }

    [Fact]
    public void Atlas2DPartitioner_over_an_offset_sub_partition_reuses_a_freed_slot()
    {
        // A 256x256 region at (256, 256) of a 512x512 image, offset on both axes
        var full = FullImagePartition(512, 512);
        var backing = full with
        {
            Extent = new VkExtent3D { Width = 256, Height = 256, Depth = 1 },
            Offset = new VkOffset3D { X = 256, Y = 256, Z = 0 },
        };
        var atlas = new Atlas2DPartitioner(backing);

        var created = new List<ImageViewPartitionInfo>();
        var request = new ImageViewPartitionInfo.CreateInfo(new VkExtent3D { Width = 128, Height = 128, Depth = 1 }, 1);
        while (created.Count < 64 && atlas.TryCreatePartition(request, out var part))
            created.Add(part);

        Assert.Equal(4, created.Count);
        Assert.All(created, p => Assert.True(Inside(p, backing), $"partition at ({p.Offset.X},{p.Offset.Y}) is outside the backing region"));

        // Free a partition that is offset within its row as well as by the backing region
        var freed = created.Single(p => p.Offset.X == 384 && p.Offset.Y == 256);
        atlas.FreePartition(freed);

        Assert.True(atlas.TryCreatePartition(request, out var reused), "the freed slot was not returned to the atlas");
        Assert.Equal(freed.Offset, reused.Offset);
    }
}
