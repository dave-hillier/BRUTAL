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

    static InMemoryLoader LoaderForSingleVec3Accessor(int bufferViewLength, int? byteStride, int accessorOffset, int count)
    {
        var stride = byteStride is null ? "" : $", \"byteStride\": {byteStride}";
        var json = $$"""
            {
              "asset": { "version": "2.0" },
              "buffers": [ { "byteLength": {{bufferViewLength}} } ],
              "bufferViews": [ { "buffer": 0, "byteLength": {{bufferViewLength}}{{stride}} } ],
              "accessors": [
                { "bufferView": 0, "byteOffset": {{accessorOffset}}, "componentType": 5126, "count": {{count}}, "type": "VEC3" }
              ]
            }
            """;
        var gltf = GltfUtility.DeserializeModelJson(json);
        return new InMemoryLoader(gltf, new byte[bufferViewLength]);
    }

    // glTF 2.0 3.6.2.4: an accessor fits in its bufferView when
    // accessor.byteOffset + byteStride * (count - 1) + elementSize <= bufferView.byteLength.
    [Fact]
    public void GetAccessorBufferSpan_accepts_strided_bufferView_sized_exactly_per_spec()
    {
        // Two interleaved vertices: POSITION (vec3 float) + NORMAL (vec3 float), stride 24.
        // NORMAL accessor: byteOffset 12, count 2 -> needs 12 + 24 + 12 = 48 bytes.
        var loader = LoaderForSingleVec3Accessor(48, byteStride: 24, accessorOffset: 12, count: 2);

        var span = loader.GetAccessorBufferSpan(0);

        Assert.Equal(24 + 12, span.Length);
    }

    [Fact]
    public void GetAccessorBufferSpan_strided_span_stops_at_last_element_when_view_has_trailing_bytes()
    {
        // Same accessor as above, but the bufferView carries 24 bytes after the accessor's data,
        // so slicing to the end of the view would return 60 bytes instead of 36.
        var loader = LoaderForSingleVec3Accessor(72, byteStride: 24, accessorOffset: 12, count: 2);

        var span = loader.GetAccessorBufferSpan(0);

        Assert.Equal((2 - 1) * 24 + 12, span.Length);
    }

    [Fact]
    public void GetAccessorBufferSpan_tightly_packed_span_is_count_times_element_size()
    {
        // Three vec3 floats with no byteStride, followed by 8 trailing bytes in the view.
        var loader = LoaderForSingleVec3Accessor(44, byteStride: null, accessorOffset: 0, count: 3);

        var span = loader.GetAccessorBufferSpan(0);

        Assert.Equal(3 * 12, span.Length);
    }
}
