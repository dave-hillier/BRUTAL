using System.Text;
using Brutal.Collections;
using Brutal.Strings;

namespace Brutal.ImGuiApi.Backends.Tests;

// GlfwBackend needs native ImGui, a context and a GLFW window, so it cannot be built in a unit test.
// Keep Create identical to the BackendPlatformName lines in GlfwBackend's constructor.
internal static class GlfwBackendNameMirror
{
    public static NativeList<byte> Create(string backendName)
    {
        NativeList<byte> _backendName;

        _backendName = [];
        _backendName.Count = Utf8.GetByteCount(backendName) + 1; // Count zero-fills, so the last byte is the null terminator
        Utf8.FromUtf16(backendName, _backendName.AsSpan(), out _, out _);

        return _backendName;
    }
}

public class GlfwBackendNameTests
{
    [Theory]
    [InlineData("Brutal.ImGui.GlfwBackend - GlfwVersion: 3.4.0 Cocoa NSGL")]
    [InlineData("Brutal.ImGui.GlfwBackend - GlfwVersion: café 中")]
    public void PlatformName_Buffer_HoldsUtf8NameFollowedByNul(string backendName)
    {
        using var buffer = GlfwBackendNameMirror.Create(backendName);

        var expected = Encoding.UTF8.GetBytes(backendName + "\0");
        Assert.Equal(expected, buffer.AsSpan().ToArray());
    }
}
