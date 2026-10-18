using Brutal.GltfApi;

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

public class GltfUtilityTests
{
    [Fact]
    public void Unpack_writes_external_images_into_output_directory_with_relative_uri()
    {
        using var input = new TempDir();
        using var output = new TempDir();
        System.IO.File.WriteAllBytes(input.File("tex.png"), new byte[] { 0x89, 0x50, 0x4E, 0x47 });
        var model = GltfUtility.DeserializeModelJson(
            """
            {
              "asset": { "version": "2.0" },
              "images": [ { "uri": "tex.png" } ]
            }
            """
        );
        var glbPath = input.File("model.glb");
        using (var fs = System.IO.File.Create(glbPath))
            model.SaveBinaryModel(null!, fs);

        GltfUtility.Unpack(glbPath, output.Path);

        var unpacked = GltfUtility.LoadModel(output.File("model.gltf"));
        var uri = unpacked.Images[0].Uri;
        Assert.False(System.IO.Path.IsPathRooted(uri), $"image uri should be relative, was '{uri}'");
        Assert.True(System.IO.File.Exists(System.IO.Path.Combine(output.Path, uri)));
    }
}
