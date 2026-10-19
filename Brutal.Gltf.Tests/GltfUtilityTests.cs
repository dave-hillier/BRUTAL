using System.Text.Json;
using Brutal.GltfApi;

namespace Brutal.Gltf.Tests;

public class GltfUtilityTests
{
    private static JsonElement RoundTrip(string json)
    {
        var output = GltfUtility.DeserializeModelJson(json).SerializeModelJson();
        using var doc = JsonDocument.Parse(output);
        return doc.RootElement.Clone();
    }

    // glTF enum strings are case-sensitive
    [Theory]
    [InlineData("SCALAR")]
    [InlineData("VEC2")]
    [InlineData("VEC3")]
    [InlineData("VEC4")]
    [InlineData("MAT2")]
    [InlineData("MAT3")]
    [InlineData("MAT4")]
    public void SerializeModelJson_writes_spec_accessor_type(string type)
    {
        var root = RoundTrip(
            $$"""
            {
              "asset": { "version": "2.0" },
              "accessors": [ { "componentType": 5126, "count": 1, "type": "{{type}}" } ]
            }
            """
        );

        Assert.Equal(type, root.GetProperty("accessors")[0].GetProperty("type").GetString());
    }

    [Theory]
    [InlineData("perspective")]
    [InlineData("orthographic")]
    public void SerializeModelJson_writes_spec_camera_type(string type)
    {
        var root = RoundTrip(
            $$"""
            {
              "asset": { "version": "2.0" },
              "cameras": [ { "type": "{{type}}" } ]
            }
            """
        );

        Assert.Equal(type, root.GetProperty("cameras")[0].GetProperty("type").GetString());
    }

    // OPAQUE is the schema default and is omitted
    [Theory]
    [InlineData("MASK")]
    [InlineData("BLEND")]
    public void SerializeModelJson_writes_spec_material_alpha_mode(string alphaMode)
    {
        var root = RoundTrip(
            $$"""
            {
              "asset": { "version": "2.0" },
              "materials": [ { "alphaMode": "{{alphaMode}}" } ]
            }
            """
        );

        Assert.Equal(alphaMode, root.GetProperty("materials")[0].GetProperty("alphaMode").GetString());
    }

    // LINEAR is the schema default and is omitted
    [Theory]
    [InlineData("STEP")]
    [InlineData("CUBICSPLINE")]
    public void SerializeModelJson_writes_spec_sampler_interpolation(string interpolation)
    {
        var root = RoundTrip(
            $$"""
            {
              "asset": { "version": "2.0" },
              "animations": [ {
                "channels": [ { "sampler": 0, "target": { "node": 0, "path": "translation" } } ],
                "samplers": [ { "input": 0, "output": 1, "interpolation": "{{interpolation}}" } ]
              } ]
            }
            """
        );

        Assert.Equal(
            interpolation,
            root.GetProperty("animations")[0].GetProperty("samplers")[0].GetProperty("interpolation").GetString()
        );
    }

    [Theory]
    [InlineData("translation")]
    [InlineData("rotation")]
    [InlineData("scale")]
    [InlineData("weights")]
    public void SerializeModelJson_writes_spec_channel_target_path(string path)
    {
        var root = RoundTrip(
            $$"""
            {
              "asset": { "version": "2.0" },
              "animations": [ {
                "channels": [ { "sampler": 0, "target": { "node": 0, "path": "{{path}}" } } ],
                "samplers": [ { "input": 0, "output": 1 } ]
              } ]
            }
            """
        );

        Assert.Equal(
            path,
            root.GetProperty("animations")[0]
                .GetProperty("channels")[0]
                .GetProperty("target")
                .GetProperty("path")
                .GetString()
        );
    }

    // glTF 2.0 5.25: an animated node MUST NOT have a matrix
    [Fact]
    public void SerializeModelJson_does_not_write_matrix_for_a_TRS_node()
    {
        var root = RoundTrip(
            """
            {
              "asset": { "version": "2.0" },
              "nodes": [ { "translation": [ 1, 2, 3 ] } ]
            }
            """
        );

        var node = root.GetProperty("nodes")[0];
        Assert.False(node.TryGetProperty("matrix", out _), $"node serialized as {node}");
    }

    [Fact]
    public void SerializeModelJson_omits_every_default_material_property()
    {
        var root = RoundTrip(
            """
            {
              "asset": { "version": "2.0" },
              "materials": [ { } ]
            }
            """
        );

        Assert.Equal("{}", root.GetProperty("materials")[0].GetRawText());
    }

    // KHR_lights_punctual: lights live only under the root "extensions" object.
    [Fact]
    public void SerializeModelJson_does_not_write_LightData_helper_property()
    {
        var root = RoundTrip(
            """
            {
              "asset": { "version": "2.0" },
              "extensions": { "KHR_lights_punctual": { "lights": [ { "type": "point" } ] } }
            }
            """
        );

        Assert.Equal(["asset", "extensions"], root.EnumerateObject().Select(p => p.Name));
    }

    // KHR_lights_punctual: a node references its light only through extensions.KHR_lights_punctual.light.
    [Fact]
    public void SerializeModelJson_does_not_write_LightIndex_helper_property()
    {
        var root = RoundTrip(
            """
            {
              "asset": { "version": "2.0" },
              "nodes": [ { "extensions": { "KHR_lights_punctual": { "light": 0 } } } ]
            }
            """
        );

        var node = root.GetProperty("nodes")[0];
        Assert.Equal(["extensions"], node.EnumerateObject().Select(p => p.Name));
    }
}
