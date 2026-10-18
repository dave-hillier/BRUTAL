using System.Runtime.InteropServices;
using Brutal.GltfApi;

namespace Brutal.Gltf.Tests;

public sealed class TempDir : IDisposable
{
    public string Path { get; } = Directory.CreateTempSubdirectory("brutal-gltf-").FullName;

    public string File(string name) => System.IO.Path.Combine(Path, name);

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, true);
        }
        catch { }
    }
}

public class GltfLoaderExtensionsTests
{
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
}
