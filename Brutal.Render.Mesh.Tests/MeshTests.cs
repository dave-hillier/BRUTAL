using Brutal.Collections;

namespace Brutal.Render.Mesh.Tests;

public class MeshTests
{
    [Fact]
    public void EnsureVertexBuffer_disposes_the_replaced_list_when_the_stride_changes()
    {
        using var mesh = new Mesh<int>();
        var original = mesh.EnsureVertexBuffer(0, ElementCount.Of(4), ByteSize.Of(12));

        var replacement = mesh.EnsureVertexBuffer(0, ElementCount.Of(4), ByteSize.Of(16));

        Assert.NotSame(original, replacement);
        // Same behaviour as EnsureIndexBuffer, which disposes through SetIndexBuffer
        Assert.True(original.IsNull(), "the replaced vertex list still owns its native memory (leaked)");
    }
}
