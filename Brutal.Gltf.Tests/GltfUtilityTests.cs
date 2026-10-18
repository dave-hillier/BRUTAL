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
    static string WriteGltf(TempDir dir, string json)
    {
        var path = dir.File("model.gltf");
        System.IO.File.WriteAllText(path, json);
        return path;
    }

    static string DataUri(byte[] bytes) => "data:application/octet-stream;base64," + Convert.ToBase64String(bytes);

    [Fact]
    public void LoadBinaryBuffer_percent_decodes_relative_uri()
    {
        // Mirrors the Khronos "Box With Spaces" sample: uri "Box%20With%20Spaces.bin".
        using var dir = new TempDir();
        var bin = new byte[] { 1, 2, 3, 4 };
        System.IO.File.WriteAllBytes(dir.File("Box With Spaces.bin"), bin);
        var path = WriteGltf(
            dir,
            """
            {
              "asset": { "version": "2.0" },
              "buffers": [ { "byteLength": 4, "uri": "Box%20With%20Spaces.bin" } ]
            }
            """
        );
        var model = GltfUtility.LoadModel(path);

        var loaded = model.LoadBinaryBuffer(0, path);

        Assert.Equal(bin, loaded);
    }

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

    [Fact]
    public void Unpack_handles_glb_whose_first_buffer_is_not_the_BIN_chunk()
    {
        using var input = new TempDir();
        using var output = new TempDir();
        var bytes = new byte[] { 1, 2, 3, 4 };
        var model = GltfUtility.DeserializeModelJson(
            $$"""
            {
              "asset": { "version": "2.0" },
              "buffers": [ { "byteLength": 4, "uri": "{{DataUri(bytes)}}" } ],
              "bufferViews": [ { "buffer": 0, "byteLength": 4 } ]
            }
            """
        );
        var glbPath = input.File("model.glb");
        using (var fs = System.IO.File.Create(glbPath))
            model.SaveBinaryModel(null!, fs);

        var ex = Record.Exception(() => GltfUtility.Unpack(glbPath, output.Path));

        Assert.Null(ex);
    }
}
