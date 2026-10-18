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
}
