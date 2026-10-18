using System.Runtime.InteropServices;
using Brutal.GltfApi;
using Brutal.Numerics;

namespace Brutal.Gltf.Tests;

public class GltfLoaderExtensionsTests
{
    // A valid glTF 2.0 model: accessors and meshes are optional top-level properties.
    const string NodeOnlyModel = """
        {
          "asset": { "version": "2.0" },
          "nodes": [ { "translation": [ 1, 2, 3 ] } ]
        }
        """;

    static byte[] Floats(params float[] values) => MemoryMarshal.AsBytes(values.AsSpan()).ToArray();

    static float[] AsFloats(byte[] bytes, int count) =>
        MemoryMarshal.Cast<byte, float>(bytes.AsSpan(0, count * 4)).ToArray();

    static string DataUri(byte[] bytes) => "data:application/octet-stream;base64," + Convert.ToBase64String(bytes);

    static string WriteGltf(TempDir dir, string json)
    {
        var path = dir.File("model.gltf");
        System.IO.File.WriteAllText(path, json);
        return path;
    }

    [Fact]
    public void FlipZAxis_also_flips_morph_target_position_displacements()
    {
        using var dir = new TempDir();
        var positions = Floats(1, 2, 3);
        var deltas = Floats(0, 0, 10);
        var data = positions.Concat(deltas).ToArray();
        var json = $$"""
            {
              "asset": { "version": "2.0" },
              "buffers": [ { "byteLength": {{data.Length}}, "uri": "{{DataUri(data)}}" } ],
              "bufferViews": [
                { "buffer": 0, "byteLength": 12 },
                { "buffer": 0, "byteOffset": 12, "byteLength": 12 }
              ],
              "accessors": [
                { "bufferView": 0, "componentType": 5126, "count": 1, "type": "VEC3" },
                { "bufferView": 1, "componentType": 5126, "count": 1, "type": "VEC3" }
              ],
              "meshes": [ { "primitives": [ { "attributes": { "POSITION": 0 }, "targets": [ { "POSITION": 1 } ] } ], "weights": [ 1.0 ] } ],
              "nodes": [ { "mesh": 0 } ]
            }
            """;
        var loader = new GltfLoader(WriteGltf(dir, json));
        loader.GetOrLoadBuffer(0);

        loader.FlipZAxis();

        var result = AsFloats(loader.LoadedBuffers[0]!, 6);
        // Morphed position (1,2,13) must become (1,2,-13): base z -> -3, delta z -> -10.
        Assert.Equal(new float[] { 1, 2, -3, 0, 0, -10 }, result);
    }

    [Fact]
    public void FlipZAxis_on_a_valid_model_without_meshes_does_not_throw()
    {
        using var dir = new TempDir();
        var loader = new GltfLoader(WriteGltf(dir, NodeOnlyModel));

        var ex = Record.Exception(() => loader.FlipZAxis());

        Assert.Null(ex);
        Assert.Equal(-3f, loader.GltfJson.Nodes[0].Translation.Z);
    }

    [Fact]
    public void SwapYZAxes_on_a_valid_model_without_meshes_does_not_throw()
    {
        using var dir = new TempDir();
        var loader = new GltfLoader(WriteGltf(dir, NodeOnlyModel));

        var ex = Record.Exception(() => loader.SwapYZAxes());

        Assert.Null(ex);
        Assert.Equal(new float3(1, 3, 2), loader.GltfJson.Nodes[0].Translation);
    }

    [Fact]
    public void Scale_on_a_valid_model_without_meshes_does_not_throw()
    {
        using var dir = new TempDir();
        var loader = new GltfLoader(WriteGltf(dir, NodeOnlyModel));

        var ex = Record.Exception(() => loader.Scale(2f));

        Assert.Null(ex);
        Assert.Equal(new float3(2, 4, 6), loader.GltfJson.Nodes[0].Translation);
    }
}
