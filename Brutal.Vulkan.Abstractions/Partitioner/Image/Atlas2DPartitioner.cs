using System.Numerics;

namespace Brutal.VulkanApi.Abstractions;

/// <summary>
/// Atlas allocator that allocates texture space in a 2D texture atlas
/// This variant divides the atlas into rows of different sizes and packs similar sized textures into appropriate rows.
/// </summary>
public class Atlas2DPartitioner : IRecycleImageViewPartitioner, ISize2D
{
    public ImageViewPartitionInfo BackingPartition { get; set; }

    protected class Row(int yPos, int texSize, int atlasSize)
    {
        public readonly int YPos = yPos;
        public readonly int TotalSlots = atlasSize / texSize;
        public readonly int TexSize = texSize;
        public readonly FreeListIndexPool FreeListIndexPool = new(atlasSize / texSize, false);
    }

    protected readonly List<Row> Rows = [];
    protected int RemainingSpace;

    /// <summary>
    /// Width of atlas, i.e. the length of each row
    /// </summary>
    public int AtlasSize { get; private set; }

    /// <inheritdoc />
    public int Width => AtlasSize;

    /// <inheritdoc />
    public int Height { get; private set; }

    /// <summary>
    /// Constructor
    /// </summary>
    public Atlas2DPartitioner(ImageViewPartitionInfo backingPartition)
    {
        BackingPartition = backingPartition;
        // Size from the backing partition, not the whole image, which may be shared with other owners
        AtlasSize = backingPartition.Extent.Width;
        Height = backingPartition.Extent.Height;
        RemainingSpace = Height;
    }

    private static bool AllocateRow(Row row, out int2 offset)
    {
        if (!row.FreeListIndexPool.TryAdd(out var slot))
        {
            offset = new int2();
            return false;
        }

        if (slot >= row.TotalSlots)
        {
            // The pool handed out an index beyond the row; give it back so it isn't leaked
            row.FreeListIndexPool.Remove(slot);
            offset = new int2();
            return false;
        }

        offset = new int2(row.TexSize * slot, row.YPos);
        return true;
    }

    private bool TryAllocate(int width, int height, out int x, out int y)
    {
        // Round up to the nearest power of 2, clamping to 1 so a zero-sized request doesn't divide by zero
        var size = (int)BitOperations.RoundUpToPowerOf2(Math.Max(1u, (uint)Math.Max(width, height)));
        // Find a row with the right size or create a new one, avoiding a LINQ enumerator allocation on this hot path
        for (var i = 0; i < Rows.Count; i++)
        {
            var row = Rows[i];
            if (row.TexSize != size)
                continue;

            if (!AllocateRow(row, out var offset))
                continue;

            x = offset.X;
            y = offset.Y;
            return true;
        }

        if (size > AtlasSize || size > RemainingSpace)
        {
            x = -1;
            y = -1;
            return false;
        }

        Rows.Add(new Row(Height - RemainingSpace, size, AtlasSize));
        RemainingSpace -= size;

        if (!AllocateRow(Rows[^1], out var offset2))
            throw new Exception("AtlasAllocator: Internal error");
        x = offset2.X;
        y = offset2.Y;
        return true;
    }

    private void Free(int x, int y)
    {
        foreach (var row in Rows)
        {
            if (y != row.YPos)
                continue;
            var slot = x / row.TexSize;
            row.FreeListIndexPool.Remove(slot);
            return;
        }

        throw new Exception("AtlasAllocator: Invalid offset");
    }

    public void Reset(ImageViewPartitionInfo info)
    {
        BackingPartition = info;
        AtlasSize = info.Extent.Width;
        Height = info.Extent.Height;
        RemainingSpace = Height;
        // Discard the old rows: their positions were carved out of the previous vertical space
        Rows.Clear();
    }

    public bool TryCreatePartition(
        in ImageViewPartitionInfo.CreateInfo createInfo,
        out ImageViewPartitionInfo partition
    )
    {
        if (createInfo.Extent.Depth != 1)
            throw new ArgumentException("Depth must be 1");
        if (createInfo.LayerCount != 1)
            throw new ArgumentException("LayerCount must be 1");

        if (!TryAllocate(createInfo.Extent.Width, createInfo.Extent.Height, out var x, out var y))
        {
            partition = default;
            return false;
        }

        partition = BackingPartition with
        {
            Partitioner = this,
            Extent = createInfo.Extent,
            Offset = new VkOffset3D
            {
                X = BackingPartition.Offset.X + x,
                Y = BackingPartition.Offset.Y + y,
                Z = BackingPartition.Offset.Z,
            },
            ArrayCount = 1,
            ArrayOffset = BackingPartition.ArrayOffset,
        };

        return true;
    }

    public void FreePartition(in ImageViewPartitionInfo inAllocation)
    {
        Free(
            inAllocation.Offset.X - BackingPartition.Offset.X,
            inAllocation.Offset.Y - BackingPartition.Offset.Y
        );
    }
}
