using Brutal.Collections;
using Brutal.Numerics;

namespace Brutal.Render.Mesh.Tests;

public class ProcGenMeshLibraryTests
{
    private static ProcGenMesh NewMesh(bool indices = false, bool normals = false) =>
        new(
            new NativeStrideList(ByteSize.Of<float3>()),
            indices ? new NativeStrideList(ByteSize.Of<uint>()) : null,
            normals ? new NativeStrideList(ByteSize.Of<float3>()) : null,
            null
        );

    [Fact]
    public void GenerateCylinder_with_normals_places_vertices_on_the_radius_and_emits_unit_normals()
    {
        var mesh = NewMesh(normals: true);
        const float radius = 2f;
        ProcGenMeshLibrary.GenerateCylinder(mesh, float3.Zero, float3.UnitY, radius, 1f, 8, addCaps: false);

        var positions = mesh.OutPositions.ToSpan<float3>().ToArray();
        var normals = mesh.OutNormals!.ToSpan<float3>().ToArray();
        foreach (var p in positions)
            Assert.Equal(radius, MathF.Sqrt(p.X * p.X + p.Z * p.Z), 3);
        foreach (var n in normals)
            Assert.Equal(1f, n.Length(), 3);
    }

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

    [Fact]
    public void CalculateTangents_from_a_downward_normal_is_finite_and_perpendicular()
    {
        using var tangents = new NativeStrideList(ByteSize.Of<float4>());
        float3[] normals = [-float3.UnitY];
        ProcGenMeshLibrary.CalculateTangents(tangents, normals);

        var t = tangents.ToSpan<float4>()[0];
        Assert.False(float.IsNaN(t.X) || float.IsNaN(t.Y) || float.IsNaN(t.Z), $"tangent was {t}");
        Assert.Equal(1f, new float3(t.X, t.Y, t.Z).Length(), 3);
        Assert.Equal(0f, float3.Dot(new float3(t.X, t.Y, t.Z), normals[0]), 3);
    }

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

    [Fact]
    public void SwizzleVertices_writes_to_the_output_span_and_leaves_the_input_untouched()
    {
        float[] input = [1, 2, 3];
        var output = new float[3];
        int[] offsets = [2, 1, 0];

        ProcGenMeshLibrary.SwizzleVertices<float>(input, output, offsets);

        Assert.Equal([3f, 2f, 1f], output);
        Assert.Equal([1f, 2f, 3f], input);
    }

    [Fact]
    public void GenerateCone_emits_unit_normals()
    {
        var mesh = NewMesh(normals: true);
        ProcGenMeshLibrary.GenerateCone(mesh, float3.Zero, float3.UnitY, 1f, 1f, 4, addCap: false);

        foreach (var n in mesh.OutNormals!.ToSpan<float3>())
            Assert.Equal(1f, n.Length(), 3);
    }
    [Theory]
    [InlineData(0f, -0.5f, 0f)]
    [InlineData(0f, 0.5f, 0f)]
    [InlineData(0f, -0.99999994f, 0f)]
    public void CalculateTangents_from_a_vertical_normal_that_is_not_unit_length_is_finite_and_perpendicular(
        float x,
        float y,
        float z
    )
    {
        using var tangents = new NativeStrideList(ByteSize.Of<float4>());
        float3[] normals = [new(x, y, z)];
        ProcGenMeshLibrary.CalculateTangents(tangents, normals);

        var t = tangents.ToSpan<float4>()[0];
        var tangent = new float3(t.X, t.Y, t.Z);
        Assert.Equal(1f, tangent.Length(), 3);
        Assert.Equal(0f, float3.Dot(tangent, float3.Normalize(normals[0])), 3);
    }

    [Fact]
    public void CalculateTangents_for_a_scaled_cube_is_finite_and_perpendicular()
    {
        var mesh = NewMesh(indices: true, normals: true);
        ProcGenMeshLibrary.GenerateCube(mesh, float3.Zero, 1f, 2);
        // Scales the normals without renormalising them, so the top and bottom face normals are (0, +-0.5, 0)
        ProcGenMeshLibrary.ScaleWorldCoords(mesh, new float3(0.5f));

        using var tangents = new NativeStrideList(ByteSize.Of<float4>());
        ProcGenMeshLibrary.CalculateTangents(mesh, tangents);

        var normals = mesh.OutNormals!.ToSpan<float3>().ToArray();
        var result = tangents.ToSpan<float4>().ToArray();
        Assert.Equal(normals.Length, result.Length);
        for (var i = 0; i < normals.Length; i++)
        {
            var tangent = new float3(result[i].X, result[i].Y, result[i].Z);
            Assert.Equal(1f, tangent.Length(), 3);
            Assert.Equal(0f, float3.Dot(tangent, float3.Normalize(normals[i])), 3);
        }
    }

    [Fact]
    public void GenerateCone_with_an_axis_just_off_vertical_emits_unit_normals()
    {
        var mesh = NewMesh(normals: true);
        var axis = float3.Normalize(new float3(0.01f, 1, 0));
        ProcGenMeshLibrary.GenerateCone(mesh, float3.Zero, axis, 2f, 1f, 8, addCap: false);

        foreach (var n in mesh.OutNormals!.ToSpan<float3>())
            Assert.Equal(1f, n.Length(), 4);
    }

    [Fact]
    public void GeneratePlane_with_a_normal_just_off_vertical_lies_perpendicular_to_the_normal()
    {
        var mesh = NewMesh(indices: true);
        var normal = float3.Normalize(new float3(0.01f, 1, 0));
        const float size = 2f;
        ProcGenMeshLibrary.GeneratePlane(mesh, float3.Zero, normal, size, 1);

        var p = mesh.OutPositions.ToSpan<float3>().ToArray();
        Assert.Equal(0f, float3.Dot(p[1] - p[0], normal), 4);
        Assert.Equal(0f, float3.Dot(p[2] - p[0], normal), 4);
        Assert.Equal(size, (p[1] - p[0]).Length(), 4);
        Assert.Equal(size, (p[2] - p[0]).Length(), 4);
    }
}
