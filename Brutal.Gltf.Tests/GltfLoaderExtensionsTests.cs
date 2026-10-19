using System.Runtime.InteropServices;
using Brutal.GltfApi;
using Brutal.Numerics;

namespace Brutal.Gltf.Tests;

public class GltfLoaderExtensionsTests
{
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

    // Buffer layout: base position | base delta (optional) | sparse index (uint) | sparse value
    static GltfLoader LoadSparseMorphModel(TempDir dir, float[]? baseDelta, float[] sparseDelta)
    {
        var positions = Floats(1, 2, 3);
        var deltas = baseDelta is null ? [] : Floats(baseDelta);
        var sparseIndex = MemoryMarshal.AsBytes(new uint[] { 0 }.AsSpan()).ToArray();
        var data = positions.Concat(deltas).Concat(sparseIndex).Concat(Floats(sparseDelta)).ToArray();
        var deltaView = baseDelta is null ? "" : """{ "buffer": 0, "byteOffset": 12, "byteLength": 12 },""";
        var firstSparseView = baseDelta is null ? 1 : 2;
        var deltaBufferView = baseDelta is null ? "" : """ "bufferView": 1,""";
        var json = $$"""
            {
              "asset": { "version": "2.0" },
              "buffers": [ { "byteLength": {{data.Length}}, "uri": "{{DataUri(data)}}" } ],
              "bufferViews": [
                { "buffer": 0, "byteLength": 12 },
                {{deltaView}}
                { "buffer": 0, "byteOffset": {{12 + deltas.Length}}, "byteLength": 4 },
                { "buffer": 0, "byteOffset": {{16 + deltas.Length}}, "byteLength": 12 }
              ],
              "accessors": [
                { "bufferView": 0, "componentType": 5126, "count": 1, "type": "VEC3" },
                {{{deltaBufferView}} "componentType": 5126, "count": 1, "type": "VEC3",
                  "sparse": {
                    "count": 1,
                    "indices": { "bufferView": {{firstSparseView}}, "componentType": 5125 },
                    "values": { "bufferView": {{firstSparseView + 1}} }
                  }
                }
              ],
              "meshes": [ { "primitives": [ { "attributes": { "POSITION": 0 }, "targets": [ { "POSITION": 1 } ] } ], "weights": [ 1.0 ] } ],
              "nodes": [ { "mesh": 0, "translation": [ 0, 0, 5 ] } ]
            }
            """;
        var loader = new GltfLoader(WriteGltf(dir, json));
        loader.GetOrLoadBuffer(0);
        return loader;
    }

    static float[] FloatsAt(GltfLoader loader, int byteOffset, int count) =>
        MemoryMarshal.Cast<byte, float>(loader.LoadedBuffers[0]!.AsSpan(byteOffset, count * 4)).ToArray();

    [Fact]
    public void FlipZAxis_flips_the_values_of_a_sparse_morph_target_without_a_bufferView()
    {
        using var dir = new TempDir();
        var loader = LoadSparseMorphModel(dir, baseDelta: null, sparseDelta: [0, 0, 10]);

        loader.FlipZAxis();

        Assert.Equal(new float[] { 1, 2, -3 }, FloatsAt(loader, 0, 3));
        Assert.Equal(new float[] { 0, 0, -10 }, FloatsAt(loader, 16, 3));
        Assert.Equal(new float3(0, 0, -5), loader.GltfJson.Nodes[0].Translation);
    }

    [Fact]
    public void FlipZAxis_flips_both_the_initial_and_the_substituted_values_of_a_sparse_morph_target()
    {
        using var dir = new TempDir();
        var loader = LoadSparseMorphModel(dir, baseDelta: [0, 0, 10], sparseDelta: [0, 0, 20]);

        loader.FlipZAxis();

        Assert.Equal(new float[] { 0, 0, -10 }, FloatsAt(loader, 12, 3));
        Assert.Equal(new float[] { 0, 0, -20 }, FloatsAt(loader, 28, 3));
    }

    [Fact]
    public void SwapYZAxes_swaps_the_values_of_a_sparse_morph_target_without_a_bufferView()
    {
        using var dir = new TempDir();
        var loader = LoadSparseMorphModel(dir, baseDelta: null, sparseDelta: [1, 2, 3]);

        loader.SwapYZAxes();

        Assert.Equal(new float[] { 1, 3, 2 }, FloatsAt(loader, 0, 3));
        Assert.Equal(new float[] { 1, 3, 2 }, FloatsAt(loader, 16, 3));
    }

    [Fact]
    public void Scale_scales_the_values_of_a_sparse_morph_target_without_a_bufferView()
    {
        using var dir = new TempDir();
        var loader = LoadSparseMorphModel(dir, baseDelta: null, sparseDelta: [1, 2, 3]);

        loader.Scale(2f);

        Assert.Equal(new float[] { 2, 4, 6 }, FloatsAt(loader, 0, 3));
        Assert.Equal(new float[] { 2, 4, 6 }, FloatsAt(loader, 16, 3));
    }

    // Two normalized VEC3 morph target elements, each padded to 4 bytes as vertex attributes require
    static GltfLoader LoadQuantizedMorphModel(TempDir dir, int componentType, byte[] targetData)
    {
        var data = Floats(1, 2, 3).Concat(targetData).ToArray();
        var json = $$"""
            {
              "asset": { "version": "2.0" },
              "extensionsUsed": [ "KHR_mesh_quantization" ],
              "extensionsRequired": [ "KHR_mesh_quantization" ],
              "buffers": [ { "byteLength": {{data.Length}}, "uri": "{{DataUri(data)}}" } ],
              "bufferViews": [
                { "buffer": 0, "byteLength": 12 },
                { "buffer": 0, "byteOffset": 12, "byteLength": {{targetData.Length}}, "byteStride": {{targetData.Length / 2}} }
              ],
              "accessors": [
                { "bufferView": 0, "componentType": 5126, "count": 1, "type": "VEC3" },
                { "bufferView": 1, "componentType": {{componentType}}, "normalized": true, "count": 2, "type": "VEC3" }
              ],
              "meshes": [ { "primitives": [ { "attributes": { "POSITION": 0 }, "targets": [ { "POSITION": 1 } ] } ], "weights": [ 1.0 ] } ],
              "nodes": [ { "mesh": 0, "translation": [ 0, 0, 5 ] } ]
            }
            """;
        var loader = new GltfLoader(WriteGltf(dir, json));
        loader.GetOrLoadBuffer(0);
        return loader;
    }

    static byte[] Shorts(params short[] values) => MemoryMarshal.AsBytes(values.AsSpan()).ToArray();

    static short[] ShortsAt(GltfLoader loader, int byteOffset, int count) =>
        MemoryMarshal.Cast<byte, short>(loader.LoadedBuffers[0]!.AsSpan(byteOffset, count * 2)).ToArray();

    [Fact]
    public void FlipZAxis_negates_a_normalized_short_morph_target_saturating_the_minimum()
    {
        using var dir = new TempDir();
        var loader = LoadQuantizedMorphModel(dir, 5122, Shorts(1, 2, 16384, 0, 4, 5, -32768, 0));

        loader.FlipZAxis();

        Assert.Equal(new float[] { 1, 2, -3 }, FloatsAt(loader, 0, 3));
        Assert.Equal(new short[] { 1, 2, -16384, 0, 4, 5, 32767, 0 }, ShortsAt(loader, 12, 8));
        Assert.Equal(new float3(0, 0, -5), loader.GltfJson.Nodes[0].Translation);
    }

    [Fact]
    public void SwapXZAxes_swaps_a_normalized_byte_morph_target()
    {
        using var dir = new TempDir();
        var loader = LoadQuantizedMorphModel(dir, 5120, [1, 2, 3, 0, 0xFC, 5, 6, 0x7F]);

        loader.SwapXZAxes();

        Assert.Equal(new float[] { 3, 2, 1 }, FloatsAt(loader, 0, 3));
        Assert.Equal(new byte[] { 3, 2, 1, 0, 6, 5, 0xFC, 0x7F }, loader.LoadedBuffers[0]!.AsSpan(12, 8).ToArray());
    }

    [Fact]
    public void Scale_leaves_a_quantized_morph_target_unchanged()
    {
        using var dir = new TempDir();
        var loader = LoadQuantizedMorphModel(dir, 5122, Shorts(1, 2, 3, 0, 4, 5, 6, 0));

        loader.Scale(2f);

        Assert.Equal(new float[] { 2, 4, 6 }, FloatsAt(loader, 0, 3));
        Assert.Equal(new short[] { 1, 2, 3, 0, 4, 5, 6, 0 }, ShortsAt(loader, 12, 8));
        Assert.Equal(new float3(0, 0, 10), loader.GltfJson.Nodes[0].Translation);
    }

    static GltfLoader LoadModelWithUnsupportedBasePosition(TempDir dir)
    {
        var data = Floats(1, 2, 3).Concat(Shorts(1, 2, 3, 0)).ToArray();
        var json = $$"""
            {
              "asset": { "version": "2.0" },
              "extensionsUsed": [ "KHR_mesh_quantization" ],
              "extensionsRequired": [ "KHR_mesh_quantization" ],
              "buffers": [ { "byteLength": {{data.Length}}, "uri": "{{DataUri(data)}}" } ],
              "bufferViews": [
                { "buffer": 0, "byteLength": 12 },
                { "buffer": 0, "byteOffset": 12, "byteLength": 8 }
              ],
              "accessors": [
                { "bufferView": 0, "componentType": 5126, "count": 1, "type": "VEC3" },
                { "bufferView": 1, "componentType": 5122, "count": 1, "type": "VEC3" }
              ],
              "meshes": [ { "primitives": [ { "attributes": { "POSITION": 0 } }, { "attributes": { "POSITION": 1 } } ] } ],
              "nodes": [ { "mesh": 0, "translation": [ 0, 0, 5 ] } ]
            }
            """;
        var loader = new GltfLoader(WriteGltf(dir, json));
        loader.GetOrLoadBuffer(0);
        return loader;
    }

    public static TheoryData<string> Transforms => ["FlipZAxis", "SwapYZAxes", "Scale"];

    static void Apply(GltfLoader loader, string transform)
    {
        switch (transform)
        {
            case "FlipZAxis":
                loader.FlipZAxis();
                break;
            case "SwapYZAxes":
                loader.SwapYZAxes();
                break;
            default:
                loader.Scale(2f);
                break;
        }
    }

    [Theory]
    [MemberData(nameof(Transforms))]
    public void An_unsupported_accessor_leaves_the_whole_model_untouched(string transform)
    {
        using var dir = new TempDir();
        var loader = LoadModelWithUnsupportedBasePosition(dir);
        var bufferBefore = loader.LoadedBuffers[0]!.ToArray();

        Assert.Throws<NotSupportedException>(() => Apply(loader, transform));

        Assert.Equal(bufferBefore, loader.LoadedBuffers[0]!);
        Assert.Equal(new float3(0, 0, 5), loader.GltfJson.Nodes[0].Translation);
    }
}
