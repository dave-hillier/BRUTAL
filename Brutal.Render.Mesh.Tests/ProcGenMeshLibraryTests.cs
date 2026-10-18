using Brutal.Collections;
using Brutal.Numerics;

namespace Brutal.Render.Mesh.Tests;

public class ProcGenMeshLibraryTests
{
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
}
