using Brutal.GltfApi;
using Brutal.Numerics;

namespace Brutal.Gltf.Tests;

public sealed class GltfTransformTests : IDisposable
{
    readonly string _directory = Directory.CreateTempSubdirectory("brutal-gltf-transform-").FullName;

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    // Two vertices of interleaved POSITION + NORMAL (vec3 float each), the layout from the
    // GetAccessorBufferSpan bug report: P0, N0, P1, N1.
    static readonly float[] InterleavedData = [1, 2, 3, 10, 20, 30, 4, 5, 6, 40, 50, 60];

    GltfLoader LoadModel(float[] data, int? byteStride, int normalOffset)
    {
        var bytes = new byte[data.Length * sizeof(float)];
        System.Buffer.BlockCopy(data, 0, bytes, 0, bytes.Length);
        var stride = byteStride is null ? "" : $", \"byteStride\": {byteStride}";
        var json = $$"""
            {
              "asset": { "version": "2.0" },
              "scene": 0,
              "scenes": [ { "nodes": [ 0 ] } ],
              "nodes": [ { "mesh": 0, "translation": [ 1, 2, 3 ] } ],
              "meshes": [ { "primitives": [ { "attributes": { "POSITION": 0, "NORMAL": 1 } } ] } ],
              "buffers": [ { "byteLength": {{bytes.Length}}, "uri": "data:application/octet-stream;base64,{{Convert.ToBase64String(bytes)}}" } ],
              "bufferViews": [ { "buffer": 0, "byteLength": {{bytes.Length}}{{stride}} } ],
              "accessors": [
                { "bufferView": 0, "byteOffset": 0, "componentType": 5126, "count": 2, "type": "VEC3", "min": [ -100, -100, -100 ], "max": [ 100, 100, 100 ] },
                { "bufferView": 0, "byteOffset": {{normalOffset}}, "componentType": 5126, "count": 2, "type": "VEC3" }
              ]
            }
            """;
        var path = Path.Combine(_directory, "model.gltf");
        File.WriteAllText(path, json);
        return new GltfLoader(path);
    }

    static float[] BufferFloats(GltfLoader loader)
    {
        var bytes = loader.GetOrLoadBuffer(0);
        var floats = new float[bytes.Length / sizeof(float)];
        System.Buffer.BlockCopy(bytes.ToArray(), 0, floats, 0, bytes.Length);
        return floats;
    }

    GltfLoader LoadInterleaved() => LoadModel(InterleavedData, byteStride: 24, normalOffset: 12);

    // The transforms step through accessor data with a fixed per-element stride. On an interleaved
    // bufferView they used to land on the neighbouring attribute every other step and silently
    // corrupt it, so they must refuse such accessors before touching anything.
    public static TheoryData<string> Transforms =>
        [
            nameof(GltfLoaderExtensions.FlipZAxis),
            nameof(GltfLoaderExtensions.FlipXAxis),
            nameof(GltfLoaderExtensions.SwapYZAxes),
            nameof(GltfLoaderExtensions.SwapXYAxes),
            nameof(GltfLoaderExtensions.Scale),
        ];

    static void Apply(string transform, GltfLoader loader)
    {
        switch (transform)
        {
            case nameof(GltfLoaderExtensions.FlipZAxis): loader.FlipZAxis(); break;
            case nameof(GltfLoaderExtensions.FlipXAxis): loader.FlipXAxis(); break;
            case nameof(GltfLoaderExtensions.SwapYZAxes): loader.SwapYZAxes(); break;
            case nameof(GltfLoaderExtensions.SwapXYAxes): loader.SwapXYAxes(); break;
            case nameof(GltfLoaderExtensions.Scale): loader.Scale(2f); break;
            default: throw new ArgumentOutOfRangeException(nameof(transform), transform, null);
        }
    }

    [Theory]
    [MemberData(nameof(Transforms))]
    public void Transform_rejects_interleaved_accessor_without_modifying_the_model(string transform)
    {
        using var loader = LoadInterleaved();

        var ex = Assert.Throws<NotSupportedException>(() => Apply(transform, loader));

        Assert.Contains("accessor 0", ex.Message);
        Assert.Contains("byteStride 24", ex.Message);
        Assert.Equal(InterleavedData, BufferFloats(loader));
        Assert.Equal(new float3(1, 2, 3), loader.GltfJson.Nodes[0].Translation);
    }

    [Fact]
    public void FlipZAxis_negates_z_of_tightly_packed_positions_and_normals()
    {
        // P0, P1, N0, N1 in one tightly packed view
        using var loader = LoadModel([1, 2, 3, 4, 5, 6, 10, 20, 30, 40, 50, 60], byteStride: null, normalOffset: 24);

        loader.FlipZAxis();

        Assert.Equal([1, 2, -3, 4, 5, -6, 10, 20, -30, 40, 50, -60], BufferFloats(loader));
    }

    [Fact]
    public void FlipZAxis_accepts_byteStride_equal_to_element_size()
    {
        // An explicit byteStride of 12 for vec3 float is still tightly packed.
        using var loader = LoadModel([1, 2, 3, 4, 5, 6, 10, 20, 30, 40, 50, 60], byteStride: 12, normalOffset: 24);

        loader.FlipZAxis();

        Assert.Equal([1, 2, -3, 4, 5, -6, 10, 20, -30, 40, 50, -60], BufferFloats(loader));
    }
}
