using Brutal.Collections;
using Brutal.Numerics;

namespace Brutal.Render.Mesh.Tests;

public class ProcGenMeshLibraryTests
{
    private static ProcGenMesh NewMesh(bool indices = false) =>
        new(
            new NativeStrideList(ByteSize.Of<float3>()),
            indices ? new NativeStrideList(ByteSize.Of<uint>()) : null,
            null,
            null
        );

    [Fact]
    public void GeneratePlane_with_a_tilted_normal_has_the_requested_size()
    {
        var mesh = NewMesh(indices: true);
        const float size = 2f;
        ProcGenMeshLibrary.GeneratePlane(mesh, float3.Zero, float3.Normalize(new float3(1, 1, 0)), size, 1);

        // Resolution 1 with indices: a 2x2 vertex grid, v0 -> v1 is one full edge
        var p = mesh.OutPositions.ToSpan<float3>().ToArray();
        Assert.Equal(4, p.Length);
        Assert.Equal(size, (p[1] - p[0]).Length(), 3);
        Assert.Equal(size, (p[2] - p[0]).Length(), 3);
    }

    [Fact]
    public void GenerateDisk_with_a_tilted_axis_has_the_requested_radius()
    {
        var mesh = NewMesh();
        const float radius = 3f;
        ProcGenMeshLibrary.GenerateDisk(mesh, float3.Zero, float3.Normalize(new float3(1, 1, 0)), radius, 8);

        var p = mesh.OutPositions.ToSpan<float3>().ToArray();
        // Triangles are (rim, rim, centre)
        Assert.Equal(radius, p[0].Length(), 3);
    }
}
