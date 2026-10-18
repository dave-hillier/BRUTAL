using Brutal.VulkanApi.Abstractions;

namespace Brutal.Vulkan.Abstractions.Tests;

public class VertexInputTests
{
    private struct PaddedVertex
    {
        public byte Flags;
        public float Weight;
    }

    [Fact]
    public unsafe void VertexInput_offsets_follow_the_struct_layout_including_padding()
    {
        var input = VertexInput.For<PaddedVertex>();

        Assert.Equal((int)ByteSize.OffsetOf<PaddedVertex>(nameof(PaddedVertex.Weight)), (int)input.Attributes[1].Offset);
        Assert.Equal(sizeof(PaddedVertex), (int)input.Bindings[0].Stride);
    }
}
