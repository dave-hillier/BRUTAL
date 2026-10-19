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
        Assert.True(original.IsNull(), "the replaced vertex list still owns its native memory (leaked)");
    }

    [Fact]
    public void SetVerticesFromData_disposes_the_list_it_replaces()
    {
        using var mesh = new Mesh<int>();
        mesh.SetVerticesFromData<int, float>(0, [1f, 2f, 3f]);
        var original = mesh.GetVertexList(0);

        mesh.SetVerticesFromData<int, float>(0, [4f, 5f, 6f]);
        var replacement = mesh.GetVertexList(0);

        Assert.NotSame(original, replacement);
        Assert.True(original.IsNull(), "the replaced vertex list still owns its native memory (leaked)");
        Assert.Equal([4f, 5f, 6f], replacement.ToSpan<float>().ToArray());
    }
}
