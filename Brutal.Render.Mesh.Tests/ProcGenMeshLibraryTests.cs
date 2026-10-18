using Brutal.Collections;
using Brutal.Numerics;

namespace Brutal.Render.Mesh.Tests;

public class ProcGenMeshLibraryTests
{
    private static ProcGenMesh NewMeshWithNormals() =>
        new(
            new NativeStrideList(ByteSize.Of<float3>()),
            null,
            new NativeStrideList(ByteSize.Of<float3>()),
            null
        );

    [Fact]
    public void GenerateCylinder_with_normals_places_vertices_on_the_radius_and_emits_unit_normals()
    {
        var mesh = NewMeshWithNormals();
        const float radius = 2f;
        ProcGenMeshLibrary.GenerateCylinder(mesh, float3.Zero, float3.UnitY, radius, 1f, 8, addCaps: false);

        var positions = mesh.OutPositions.ToSpan<float3>().ToArray();
        var normals = mesh.OutNormals!.ToSpan<float3>().ToArray();
        foreach (var p in positions)
            Assert.Equal(radius, MathF.Sqrt(p.X * p.X + p.Z * p.Z), 3);
        foreach (var n in normals)
            Assert.Equal(1f, n.Length(), 3);
    }
}
