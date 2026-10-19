using System.Diagnostics;
using System.Runtime.CompilerServices;
using Brutal.Concurrency.Jobs;

namespace Brutal.Concurrency.Tests;

public class JobRunnerTests : IClassFixture<JobRunnerCrashReproFixture>
{
    private readonly JobRunnerCrashReproFixture _crashRepro;

    public JobRunnerTests(JobRunnerCrashReproFixture crashRepro)
    {
        _crashRepro = crashRepro;
    }

    private sealed class BlockingJob(ManualResetEventSlim started, ManualResetEventSlim release) : IJob
    {
        public void Run()
        {
            started.Set();
            release.Wait();
        }
    }

    private sealed class NoopJob : IJob
    {
        public void Run() { }
    }

    private sealed class SlowThrowingJob(Exception exception) : IJob
    {
        public void Run()
        {
            Thread.Sleep(300);
            throw exception;
        }
    }

    private sealed class CountingJob : IJob
    {
        private int _runCount;
        public int RunCount => Volatile.Read(ref _runCount);

        public void Run() => Interlocked.Increment(ref _runCount);
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

        // Require the callback line so the test cannot pass vacuously.
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

    [Fact]
    public void FaultedRun_ReportsFaultedAndException_AndDoesNotContributeToMetrics()
    {
        var runner = new JobRunner();
        var thrown = new InvalidOperationException("job failed on purpose");
        var jobAfterThrow = new CountingJob();

        runner.Queue(new SlowThrowingJob(thrown));
        runner.Queue(jobAfterThrow);
        ExecutionResult? faultedResult = null;
        Exception? exceptionSeenByCallback = null;
        runner.ExecuteJobs(
            (r, result, _) =>
            {
                faultedResult = result;
                exceptionSeenByCallback = r.LastException;
            }
        );
        runner.Wait();

        Assert.Equal(ExecutionResult.Faulted, faultedResult);
        Assert.Same(thrown, exceptionSeenByCallback);
        Assert.Same(thrown, runner.LastException);
        Assert.Equal(0, jobAfterThrow.RunCount);
        var faultedMetrics = runner.GetMetrics();
        Assert.Equal(0, faultedMetrics.LastTickTimeMillis);
        Assert.Equal(0, faultedMetrics.MaxTickTimeMillis);
        Assert.Equal(0, faultedMetrics.AverageTickTimeMillis);

        ExecutionResult? secondResult = null;
        runner.Queue(new NoopJob());
        runner.ExecuteJobs((_, result, _) => secondResult = result);
        runner.Wait();

        Assert.Equal(ExecutionResult.Success, secondResult);
        Assert.Null(runner.LastException);
        var metrics = runner.GetMetrics();
        Assert.True(metrics.MaxTickTimeMillis < 150, $"MaxTickTimeMillis was {metrics.MaxTickTimeMillis}");
        Assert.True(metrics.AverageTickTimeMillis < 150, $"AverageTickTimeMillis was {metrics.AverageTickTimeMillis}");
    }

    [Fact]
    public void AbortedRun_DoesNotContributeToMetricsOfLaterSuccessfulRun()
    {
        var runner = new JobRunner();
        using var started = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();

        runner.Queue(new BlockingJob(started, release));
        runner.Queue(new NoopJob());
        ExecutionResult? firstResult = null;
        runner.ExecuteJobs((_, result, _) => firstResult = result);
        started.Wait();
        runner.RequestAbort();
        Thread.Sleep(300);
        release.Set();
        runner.Wait();
        Assert.Equal(ExecutionResult.Aborted, firstResult);

        runner.Queue(new NoopJob());
        runner.ExecuteJobs();
        runner.Wait();

        var metrics = runner.GetMetrics();
        Assert.True(metrics.MaxTickTimeMillis < 150, $"MaxTickTimeMillis was {metrics.MaxTickTimeMillis}");
        Assert.True(metrics.AverageTickTimeMillis < 150, $"AverageTickTimeMillis was {metrics.AverageTickTimeMillis}");
    }

    [Fact]
    public void Scheduler_AbortedRun_DoesNotContributeToMetricsOfLaterSuccessfulRun()
    {
        var scheduler = new JobScheduler(1);
        using var started = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();

        scheduler.Queue(new BlockingJob(started, release), 1);
        scheduler.Queue(new NoopJob(), 1);
        scheduler.ExecuteJobs();
        started.Wait();
        scheduler.AbortJobs();
        Thread.Sleep(300);
        release.Set();
        scheduler.Wait();

        scheduler.Queue(new NoopJob(), 1);
        scheduler.ExecuteJobs();
        scheduler.Wait();

        Span<JobRunnerMetrics> metrics = stackalloc JobRunnerMetrics[1];
        scheduler.GetMetrics(metrics);
        Assert.True(metrics[0].MaxTickTimeMillis < 150, $"MaxTickTimeMillis was {metrics[0].MaxTickTimeMillis}");
    }
}

public sealed record ProcessRun(int ExitCode, string StdOut, string StdErr);

/// <summary>
/// Builds Brutal.Concurrency.Tests.CrashRepro once per test class so it can be run out-of-process.
/// </summary>
public sealed class JobRunnerCrashReproFixture
{
    private readonly string _dllPath;

    public JobRunnerCrashReproFixture()
    {
        var projectDir = Path.GetFullPath(
            Path.Combine(ThisFileDirectory(), "..", "Brutal.Concurrency.Tests.CrashRepro")
        );
        // Not a shared temp path, so concurrent runs from different checkouts never share helper binaries.
        var artifacts = Path.Combine(AppContext.BaseDirectory, "jobrunner-crash-repro");

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
