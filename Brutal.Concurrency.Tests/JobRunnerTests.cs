using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Brutal.Concurrency.Tests;

public class JobRunnerTests : IClassFixture<JobRunnerCrashReproFixture>
{
    private readonly JobRunnerCrashReproFixture _crashRepro;

    public JobRunnerTests(JobRunnerCrashReproFixture crashRepro)
    {
        _crashRepro = crashRepro;
    }

    [Fact]
    public void ExecuteJobs_WhenAJobThrows_ProcessSurvives()
    {
        var run = _crashRepro.Run();

        Assert.True(
            run.ExitCode == 0,
            $"Process exited with code {run.ExitCode}, expected 0.\nstdout:\n{run.StdOut}\nstderr:\n{run.StdErr}"
        );
        Assert.Contains("SURVIVED", run.StdOut);
    }

    [Fact]
    public void ExecuteJobs_WhenAJobThrows_DoesNotReportSuccessWhileSkippingRemainingJobs()
    {
        var run = _crashRepro.Run();

        // The completion callback runs from DoWork's finally block, even when the process is then torn down.
        // Require the callback line so the test cannot pass vacuously if the callback never ran.
        Assert.True(
            run.StdOut.Contains("COMPLETED result="),
            $"Completion callback was never invoked.\nstdout:\n{run.StdOut}\nstderr:\n{run.StdErr}"
        );
        var reportedSuccess = run.StdOut.Contains("COMPLETED result=Success");
        var remainingJobSkipped = run.StdOut.Contains("jobsAfterThrowRan=0");

        Assert.False(
            reportedSuccess && remainingJobSkipped,
            $"Runner reported Success although the job queued after the throwing job never ran.\nstdout:\n{run.StdOut}"
        );
    }
}

public sealed record ProcessRun(int ExitCode, string StdOut, string StdErr);

/// <summary>
/// Builds Brutal.Concurrency.Tests.CrashRepro once per test class so it can be run out-of-process:
/// an exception escaping a ThreadPool work item would otherwise terminate the test host.
/// </summary>
public sealed class JobRunnerCrashReproFixture
{
    private readonly string _dllPath;

    public JobRunnerCrashReproFixture()
    {
        var projectDir = Path.GetFullPath(
            Path.Combine(ThisFileDirectory(), "..", "Brutal.Concurrency.Tests.CrashRepro")
        );
        var artifacts = Path.Combine(Path.GetTempPath(), "brutal-tests", "jobrunner-crash-repro");

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
            "Brutal.Concurrency.Tests.CrashRepro",
            "debug",
            "Brutal.Concurrency.Tests.CrashRepro.dll"
        );
    }

    public ProcessRun Run() => Execute("dotnet", [_dllPath], TimeSpan.FromSeconds(30));

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
