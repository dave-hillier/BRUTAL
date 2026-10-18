using System.Diagnostics;

namespace Brutal.Collections.Tests;

public class NativeListOfTTests
{
    private static (int ExitCode, string Output) RunScenario(string scenario)
    {
        var dll = Path.Combine(AppContext.BaseDirectory, "Brutal.Collections.Tests.CrashRepro.dll");
        var psi = new ProcessStartInfo("dotnet", $"\"{dll}\" {scenario}")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        using var process = Process.Start(psi)!;
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return (process.ExitCode, stdout + stderr);
    }

    [Fact]
    public void TypedNativeList_WhoseConstructorThrows_DoesNotCrashTheProcessWhenFinalized()
    {
        var (exitCode, output) = RunScenario("typed-list-ctor-throws");

        Assert.True(exitCode == 0, $"exit code {exitCode}: {output}");
    }
}
