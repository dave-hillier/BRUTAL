using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Brutal.Strings.Tests;

/// <summary>
/// Utf8.Path behaviour on invalid UTF-8 and on very long paths. A stack overflow terminates the process, so
/// each long-path call runs out-of-process in Brutal.Strings.Tests.CrashRepro on a worker thread with the
/// default stack size.
/// </summary>
public class Utf8PathTests : IClassFixture<Utf8PathCrashReproFixture>
{
    private const int LongPathByteLength = 1_000_000;

    private readonly Utf8PathCrashReproFixture _crashRepro;

    public Utf8PathTests(Utf8PathCrashReproFixture crashRepro)
    {
        _crashRepro = crashRepro;
    }

    [Fact]
    public void GetDirectoryName_OnLongPath_DoesNotOverflowTheStack()
    {
        var run = _crashRepro.Run("GetDirectoryName", LongPathByteLength);

        AssertSurvived(run);
        Assert.Equal($"SURVIVED {LongPathByteLength - 2}", run.StdOut.Trim());
    }

    [Fact]
    public void GetFileName_OnLongPath_DoesNotOverflowTheStack()
    {
        var run = _crashRepro.Run("GetFileName", LongPathByteLength);

        AssertSurvived(run);
        Assert.Equal("SURVIVED 1", run.StdOut.Trim());
    }

    [Fact]
    public void IsPathFullyQualified_OnLongPath_DoesNotOverflowTheStack()
    {
        var run = _crashRepro.Run("IsPathFullyQualified", LongPathByteLength);

        AssertSurvived(run);
        Assert.Equal("SURVIVED False", run.StdOut.Trim());
    }

    [Fact]
    public void Utf8Path_GetDirectoryName_InvalidUtf8_ThrowsOrReturnsCorrectPrefix()
    {
        byte[] path = [0xFF, (byte)'/', (byte)'a', (byte)'b'];
        AssertInvalidUtf8HandledCorrectly(() => Utf8.Path.GetDirectoryName(path).ToArray(), [0xFF]);
    }

    [Fact]
    public void Utf8Path_GetFileName_InvalidUtf8_ThrowsOrReturnsCorrectSuffix()
    {
        byte[] path = [(byte)'d', (byte)'i', (byte)'r', (byte)'/', 0xFF];
        AssertInvalidUtf8HandledCorrectly(() => Utf8.Path.GetFileName(path).ToArray(), [0xFF]);
    }

    private static void AssertInvalidUtf8HandledCorrectly(Func<byte[]> call, byte[] expected)
    {
        byte[]? result = null;
        Exception? ex = null;
        try
        {
            result = call();
        }
        catch (Exception e)
        {
            ex = e;
        }

        var ok = ex?.GetType() == typeof(ArgumentException) || (result is not null && result.AsSpan().SequenceEqual(expected));
        Assert.True(
            ok,
            $"Expected ArgumentException (documented 'Invalid UTF-8 path') or [{Convert.ToHexString(expected)}], "
                + $"got {(ex is null ? "[" + Convert.ToHexString(result!) + "]" : ex.GetType().Name + ": " + ex.Message)}"
        );
    }

    private static void AssertSurvived(ProcessRun run) =>
        Assert.True(
            run.ExitCode == 0,
            $"Process exited with code {run.ExitCode}, expected 0.\nstdout:\n{run.StdOut}\nstderr:\n{run.StdErr}"
        );
}

public sealed record ProcessRun(int ExitCode, string StdOut, string StdErr);

/// <summary>
/// Builds Brutal.Strings.Tests.CrashRepro once per test class so it can be run out-of-process.
/// </summary>
public sealed class Utf8PathCrashReproFixture
{
    private readonly string _dllPath;

    public Utf8PathCrashReproFixture()
    {
        var projectDir = Path.GetFullPath(
            Path.Combine(ThisFileDirectory(), "..", "Brutal.Strings.Tests.CrashRepro")
        );
        // Build under this test assembly's own output directory so concurrent runs from other checkouts or
        // artifact paths never share (and race on) the helper's bin/obj.
        var artifacts = Path.Combine(AppContext.BaseDirectory, "utf8path-crash-repro");

        var build = Execute(
            "dotnet",
            [
                "build",
                projectDir,
                "--artifacts-path",
                artifacts,
                "--disable-build-servers",
                "-p:UseSharedCompilation=false",
                "-nologo",
                "-v",
                "q",
            ],
            TimeSpan.FromMinutes(3)
        );
        if (build.ExitCode != 0)
            throw new InvalidOperationException(
                $"Building the crash repro helper failed:\n{build.StdOut}\n{build.StdErr}"
            );

        _dllPath = Path.Combine(
            artifacts,
            "bin",
            "Brutal.Strings.Tests.CrashRepro",
            "debug",
            "Brutal.Strings.Tests.CrashRepro.dll"
        );
    }

    public ProcessRun Run(string method, int pathByteLength) =>
        Execute("dotnet", [_dllPath, method, pathByteLength.ToString()], TimeSpan.FromSeconds(30));

    private static ProcessRun Execute(string fileName, string[] args, TimeSpan timeout)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var arg in args)
            startInfo.ArgumentList.Add(arg);
        // Do not let the child inherit the test host's MSBuild settings.
        foreach (var key in startInfo.Environment.Keys.ToList())
        {
            if (key.StartsWith("MSBUILD", StringComparison.OrdinalIgnoreCase))
                startInfo.Environment.Remove(key);
        }

        using var process = Process.Start(startInfo)!;
        var stdOut = process.StandardOutput.ReadToEndAsync();
        var stdErr = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(timeout))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException($"{fileName} {string.Join(' ', args)} timed out");
        }
        return new ProcessRun(process.ExitCode, stdOut.Result, stdErr.Result);
    }

    private static string ThisFileDirectory([CallerFilePath] string path = "") =>
        Path.GetDirectoryName(path)!;
}
