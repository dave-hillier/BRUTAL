using Brutal.Collections;
using Brutal.Numerics;

namespace Brutal.Render.Mesh.Tests;

public class ProcGenMeshLibraryTests
{
    private static ProcGenMesh NewMesh(bool normals = false) =>
        new(
            new NativeStrideList(ByteSize.Of<float3>()),
            null,
            normals ? new NativeStrideList(ByteSize.Of<float3>()) : null,
            null
        );

    [Fact]
    public void GenerateCone_emits_unit_normals()
    {
        var mesh = NewMesh(normals: true);
        ProcGenMeshLibrary.GenerateCone(mesh, float3.Zero, float3.UnitY, 1f, 1f, 4, addCap: false);

        foreach (var n in mesh.OutNormals!.ToSpan<float3>())
            Assert.Equal(1f, n.Length(), 3);
    }
}
