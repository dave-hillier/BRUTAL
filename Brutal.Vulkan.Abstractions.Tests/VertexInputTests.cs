using Brutal.Numerics;
using Brutal.VulkanApi;
using Brutal.VulkanApi.Abstractions;

namespace Brutal.Vulkan.Abstractions.Tests;

public class VertexInputTests
{
    private struct NormalizedColourVertex
    {
        [FixedPointConversion(Behavior.Normalize)]
        public ushort4 Colour;

        public float3 Position;
    }

    private struct NormalizedPairVertex
    {
        [FixedPointConversion(Behavior.Normalize)]
        public byte A;

        [FixedPointConversion(Behavior.Normalize)]
        public byte B;

        public ushort C;
    }

    [Fact]
    public unsafe void VertexInput_normalized_ushort4_field_advances_by_its_buffer_size()
    {
        var input = VertexInput.For<NormalizedColourVertex>();

        Assert.Equal(VkFormat.R16G16B16A16UNorm, input.Attributes[0].Format);
        Assert.Equal((int)ByteSize.OffsetOf<NormalizedColourVertex>(nameof(NormalizedColourVertex.Position)), (int)input.Attributes[1].Offset);
        Assert.Equal(sizeof(NormalizedColourVertex), (int)input.Bindings[0].Stride);
    }

    [Fact]
    public unsafe void VertexInput_normalized_bytes_are_packed_at_their_field_offsets()
    {
        var input = VertexInput.For<NormalizedPairVertex>();

        Assert.Equal(1, (int)input.Attributes[1].Offset);
        Assert.Equal(2, (int)input.Attributes[2].Offset);
        Assert.Equal(sizeof(NormalizedPairVertex), (int)input.Bindings[0].Stride);
    }
}
