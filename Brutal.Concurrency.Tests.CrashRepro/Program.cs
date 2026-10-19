using Brutal.Concurrency.Jobs;

// Runs a throwing job out of process, since an exception escaping a ThreadPool work item
// terminates the process. JobRunnerTests checks the exit code and the COMPLETED/SURVIVED lines.

var jobAfterThrow = new CountingJob();
var runner = new JobRunner("crash-repro");
runner.Queue(new ThrowingJob());
runner.Queue(jobAfterThrow);

runner.ExecuteJobs(
    (_, result, _) =>
        Console.WriteLine($"COMPLETED result={result} jobsAfterThrowRan={jobAfterThrow.RunCount}")
);

if (!SpinWait.SpinUntil(() => runner.State == JobState.Idle, TimeSpan.FromSeconds(10)))
{
    Console.WriteLine("TIMEOUT");
    return 2;
}

// Give a crashing ThreadPool thread time to tear the process down if it is going to.
Thread.Sleep(500);
Console.WriteLine("SURVIVED");
return 0;

sealed class ThrowingJob : IJob
{
    public void Run() => throw new InvalidOperationException("job failed on purpose");
}

sealed class CountingJob : IJob
{
    private int _runCount;
    public int RunCount => Volatile.Read(ref _runCount);

    public void Run() => Interlocked.Increment(ref _runCount);
}
