using System.Text.Json;
using Brutal.GltfApi;

namespace Brutal.Gltf.Tests;

public class GltfUtilityTests
{
    // glTF 2.0 5.25: a node MAY have either a matrix or TRS properties, and an animated node MUST NOT have a matrix.
    [Fact]
    public void SerializeModelJson_does_not_write_matrix_for_a_TRS_node()
    {
        var model = GltfUtility.DeserializeModelJson(
            """
            {
              "asset": { "version": "2.0" },
              "nodes": [ { "translation": [ 1, 2, 3 ] } ]
            }
            """
        );

        var output = model.SerializeModelJson();

        using var doc = JsonDocument.Parse(output);
        var node = doc.RootElement.GetProperty("nodes")[0];
        Assert.False(node.TryGetProperty("matrix", out _), $"node serialized as {node}");
    }
}
