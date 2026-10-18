using Brutal.GltfApi;

namespace Brutal.Gltf.Tests;

public class GltfLoaderExtensionsTests
{
    sealed class InMemoryLoader(GltfApi.Gltf gltf, byte[] buffer) : IGltfLoader
    {
        public GltfApi.Gltf GltfJson => gltf;

        public ReadOnlySpan<byte> GetBufferView(int bufferViewIndex)
        {
            var bv = gltf.BufferViews[bufferViewIndex];
            return buffer.AsSpan(bv.ByteOffset, bv.ByteLength);
        }
    }

    // glTF 2.0 3.6.2.4: an accessor fits in its bufferView when
    // accessor.byteOffset + byteStride * (count - 1) + elementSize <= bufferView.byteLength.
    [Fact]
    public void GetAccessorBufferSpan_accepts_strided_bufferView_sized_exactly_per_spec()
    {
        // Two interleaved vertices: POSITION (vec3 float) + NORMAL (vec3 float), stride 24.
        // NORMAL accessor: byteOffset 12, count 2 -> needs 12 + 24 + 12 = 48 bytes.
        var json = """
            {
              "asset": { "version": "2.0" },
              "buffers": [ { "byteLength": 48 } ],
              "bufferViews": [ { "buffer": 0, "byteLength": 48, "byteStride": 24 } ],
              "accessors": [
                { "bufferView": 0, "byteOffset": 12, "componentType": 5126, "count": 2, "type": "VEC3" }
              ]
            }
            """;
        var gltf = GltfUtility.DeserializeModelJson(json);
        var loader = new InMemoryLoader(gltf, new byte[48]);

        var span = loader.GetAccessorBufferSpan(0);

        Assert.True(span.Length >= 24 + 12);
    }
}
