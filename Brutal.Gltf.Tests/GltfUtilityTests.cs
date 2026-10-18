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
    static string DataUri(byte[] bytes) => "data:application/octet-stream;base64," + Convert.ToBase64String(bytes);

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
