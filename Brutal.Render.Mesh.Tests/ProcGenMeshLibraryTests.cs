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
    public void GenerateTorus_normals_point_away_from_the_tube_centre()
    {
        var mesh = NewMesh(normals: true);
        const float radiusA = 1f;
        const float radiusB = 0.5f;
        ProcGenMeshLibrary.GenerateTorus(mesh, float3.Zero, radiusA, radiusB, 8, 8);

        var positions = mesh.OutPositions.ToSpan<float3>().ToArray();
        var normals = mesh.OutNormals!.ToSpan<float3>().ToArray();
        for (var i = 0; i < positions.Length; i++)
        {
            var p = positions[i];
            // Centre of the tube cross-section this vertex lies on
            var ring = float3.Normalize(new float3(p.X, 0, p.Z)) * radiusA;
            var expected = float3.Normalize(p - ring);
            Assert.True(
                float3.Dot(expected, normals[i]) > 0.999f,
                $"vertex {i} at {p}: expected normal {expected}, got {normals[i]}"
            );
        }
    }
}
