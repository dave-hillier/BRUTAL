using Brutal.GltfApi;
using Brutal.Numerics;

namespace Brutal.Gltf.Tests;

public sealed class TempDir : IDisposable
{
    public string Path { get; } = Directory.CreateTempSubdirectory("brutal-gltf-").FullName;

    public string File(string name) => System.IO.Path.Combine(Path, name);

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, true);
        }
        catch { }
    }
}

public class GltfLoaderExtensionsTests
{
    // A valid glTF 2.0 model: accessors and meshes are optional top-level properties.
    const string NodeOnlyModel = """
        {
          "asset": { "version": "2.0" },
          "nodes": [ { "translation": [ 1, 2, 3 ] } ]
        }
        """;

    static string WriteGltf(TempDir dir, string json)
    {
        var path = dir.File("model.gltf");
        System.IO.File.WriteAllText(path, json);
        return path;
    }

    [Fact]
    public void FlipZAxis_on_a_valid_model_without_meshes_does_not_throw()
    {
        using var dir = new TempDir();
        var loader = new GltfLoader(WriteGltf(dir, NodeOnlyModel));

        var ex = Record.Exception(() => loader.FlipZAxis());

        Assert.Null(ex);
        Assert.Equal(-3f, loader.GltfJson.Nodes[0].Translation.Z);
    }

    [Fact]
    public void SwapYZAxes_on_a_valid_model_without_meshes_does_not_throw()
    {
        using var dir = new TempDir();
        var loader = new GltfLoader(WriteGltf(dir, NodeOnlyModel));

        var ex = Record.Exception(() => loader.SwapYZAxes());

        Assert.Null(ex);
        Assert.Equal(new float3(1, 3, 2), loader.GltfJson.Nodes[0].Translation);
    }

    [Fact]
    public void Scale_on_a_valid_model_without_meshes_does_not_throw()
    {
        using var dir = new TempDir();
        var loader = new GltfLoader(WriteGltf(dir, NodeOnlyModel));

        var ex = Record.Exception(() => loader.Scale(2f));

        Assert.Null(ex);
        Assert.Equal(new float3(2, 4, 6), loader.GltfJson.Nodes[0].Translation);
    }
}
