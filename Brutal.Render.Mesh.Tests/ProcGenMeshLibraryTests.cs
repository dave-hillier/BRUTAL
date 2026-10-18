namespace Brutal.Render.Mesh.Tests;

public class ProcGenMeshLibraryTests
{
    [Fact]
    public void SwizzleVertices_writes_to_the_output_span_and_leaves_the_input_untouched()
    {
        float[] input = [1, 2, 3];
        var output = new float[3];
        int[] offsets = [2, 1, 0];

        ProcGenMeshLibrary.SwizzleVertices<float>(input, output, offsets);

        Assert.Equal([3f, 2f, 1f], output);
        Assert.Equal([1f, 2f, 3f], input);
    }
}
