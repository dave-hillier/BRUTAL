using Brutal.Concurrency.Jobs;

namespace Brutal.Concurrency.Tests;

public class JobRunnerTests
{
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

    [Fact]
    public void AbortedRun_DoesNotContributeToMetricsOfLaterSuccessfulRun()
    {
        var runner = new JobRunner();
        using var started = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();

        // First run: a slow job followed by one that never runs because we abort.
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

        // Second run: a trivially fast successful job.
        runner.Queue(new NoopJob());
        runner.ExecuteJobs();
        runner.Wait();

        var metrics = runner.GetMetrics();
        // RequestAbort documents "Metrics will not be updated", so the ~300ms aborted run must not
        // show up in the max or the average of the next successful run.
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
