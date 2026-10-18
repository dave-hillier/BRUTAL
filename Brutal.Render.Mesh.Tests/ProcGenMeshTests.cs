using Brutal.Collections;
using Brutal.Numerics;

namespace Brutal.Render.Mesh.Tests;

public class ProcGenMeshTests
{
    private static ProcGenMesh NewMesh(bool indices = false) =>
        new(
            new NativeStrideList(ByteSize.Of<float3>()),
            indices ? new NativeStrideList(ByteSize.Of<uint>()) : null,
            null,
            null
        );

    [Fact]
    public void AddQuad_with_indices_emits_two_triangles_referencing_only_its_four_vertices()
    {
        var mesh = NewMesh(indices: true);
        ProcGenMeshLibrary.AddQuad(mesh, new float3(0, 0, 0), new float3(1, 0, 0), new float3(1, 1, 0), new float3(0, 1, 0));

        var indices = mesh.OutIndices!.ToSpan<uint>().ToArray();
        Assert.Equal(4, mesh.VertexCount);
        Assert.Equal(6, indices.Length);
        Assert.All(indices, i => Assert.InRange(i, 0u, 3u));
    }
}
