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
}
