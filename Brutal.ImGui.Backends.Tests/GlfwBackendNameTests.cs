using System.Text;
using Brutal.Collections;
using Brutal.Strings;

namespace Brutal.ImGuiApi.Backends.Tests;

// GlfwBackend cannot be constructed in a unit test: it needs the native ImGui
// library, a current ImGui context and a GLFW window (on the macOS main thread).
// These tests instead run the exact lines GlfwBackend's constructor uses to build
// the buffer it hands to ImGui as io.BackendPlatformName, against the real
// NativeList<byte> and Brutal.Strings Utf8. Keep the body of
// GlfwBackendNameMirror.Create identical to those lines in
// Brutal.ImGui.Backends/PlatformBackends/GlfwBackend.cs.
internal static class GlfwBackendNameMirror
{
    public static NativeList<byte> Create(string backendName)
    {
        NativeList<byte> _backendName;

        // Copied verbatim from GlfwBackend's constructor.
        _backendName = new NativeList<byte>(Utf8.GetByteCount(backendName) + 1);
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

        // ImGui reads io.BackendPlatformName (buffer.Ptr) as a NUL-terminated C string,
        // so the list must hold the encoded name and the terminator.
        var expected = Encoding.UTF8.GetBytes(backendName + "\0");
        Assert.Equal(expected, buffer.AsSpan().ToArray());
    }
}
