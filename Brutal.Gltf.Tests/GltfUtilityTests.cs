using System.Text.Json;
using Brutal.GltfApi;

namespace Brutal.Gltf.Tests;

public class GltfUtilityTests
{
    [Fact]
    public void SerializeModelJson_writes_spec_enum_strings()
    {
        var json = """
            {
              "asset": { "version": "2.0" },
              "accessors": [ { "componentType": 5126, "count": 1, "type": "VEC3" } ]
            }
            """;
        var model = GltfUtility.DeserializeModelJson(json);

        var output = model.SerializeModelJson();

        using var doc = JsonDocument.Parse(output);
        Assert.Equal("VEC3", doc.RootElement.GetProperty("accessors")[0].GetProperty("type").GetString());
    }

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
