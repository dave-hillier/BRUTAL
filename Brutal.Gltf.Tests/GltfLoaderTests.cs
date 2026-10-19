using System.Runtime.InteropServices;
using Brutal.GltfApi;

namespace Brutal.Gltf.Tests;

public class GltfLoaderTests
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
    public void FlipZAxis_respects_byteStride_of_interleaved_vertex_data()
    {
        using var dir = new TempDir();
        // Two interleaved position/normal vertices followed by 12 bytes of padding
        var data = Floats(1, 2, 3, 0, 0, 1, 4, 5, 6, 0, 0, -1, 0, 0, 0);
        var json = $$"""
            {
              "asset": { "version": "2.0" },
              "buffers": [ { "byteLength": {{data.Length}}, "uri": "{{DataUri(data)}}" } ],
              "bufferViews": [ { "buffer": 0, "byteLength": {{data.Length}}, "byteStride": 24 } ],
              "accessors": [
                { "bufferView": 0, "byteOffset": 0, "componentType": 5126, "count": 2, "type": "VEC3" },
                { "bufferView": 0, "byteOffset": 12, "componentType": 5126, "count": 2, "type": "VEC3" }
              ],
              "meshes": [ { "primitives": [ { "attributes": { "POSITION": 0, "NORMAL": 1 } } ] } ],
              "nodes": [ { "mesh": 0 } ]
            }
            """;
        var loader = new GltfLoader(WriteGltf(dir, json));
        loader.GetOrLoadBuffer(0);

        loader.FlipZAxis();

        var result = AsFloats(loader.LoadedBuffers[0]!, 15);
        Assert.Equal(new float[] { 1, 2, -3, 0, 0, -1, 4, 5, -6, 0, 0, 1, 0, 0, 0 }, result);
    }

    [Fact]
    public void FlipZAxis_does_not_reorder_indices_of_a_LINES_primitive()
    {
        using var dir = new TempDir();
        var positions = Floats(0, 0, 0, 1, 0, 0, 2, 0, 0, 3, 0, 0, 4, 0, 0, 5, 0, 0);
        var indices = MemoryMarshal.AsBytes(new ushort[] { 0, 1, 2, 3, 4, 5 }.AsSpan()).ToArray();
        var data = positions.Concat(indices).ToArray();
        var json = $$"""
            {
              "asset": { "version": "2.0" },
              "buffers": [ { "byteLength": {{data.Length}}, "uri": "{{DataUri(data)}}" } ],
              "bufferViews": [
                { "buffer": 0, "byteLength": {{positions.Length}} },
                { "buffer": 0, "byteOffset": {{positions.Length}}, "byteLength": {{indices.Length}} }
              ],
              "accessors": [
                { "bufferView": 0, "componentType": 5126, "count": 6, "type": "VEC3" },
                { "bufferView": 1, "componentType": 5123, "count": 6, "type": "SCALAR" }
              ],
              "meshes": [ { "primitives": [ { "attributes": { "POSITION": 0 }, "indices": 1, "mode": 1 } ] } ],
              "nodes": [ { "mesh": 0 } ]
            }
            """;
        var loader = new GltfLoader(WriteGltf(dir, json));
        loader.GetOrLoadBuffer(0);

        loader.FlipZAxis();

        var resultIndices = MemoryMarshal
            .Cast<byte, ushort>(loader.LoadedBuffers[0]!.AsSpan(positions.Length, indices.Length))
            .ToArray();
        Assert.Equal(new ushort[] { 0, 1, 2, 3, 4, 5 }, resultIndices);
    }
}
