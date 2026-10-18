using Brutal.Concurrency.Jobs;

// Out-of-process reproduction for JobRunner behaviour when a job's Run() throws.
// An exception escaping a ThreadPool work item terminates the process, so this cannot
// run inside the xUnit test host. JobRunnerTests launches this program and inspects
// its exit code and standard output.
//
// Output lines:
//   COMPLETED result=<ExecutionResult> jobsAfterThrowRan=<n>   (written by the completion callback)
//   SURVIVED                                                   (written once the runner is idle again)
// Exit code 0 means the process survived the throwing job.

var jobAfterThrow = new CountingJob();
var runner = new JobRunner("crash-repro");
runner.Queue(new ThrowingJob());
runner.Queue(jobAfterThrow);

runner.ExecuteJobs(
    (_, result, _) =>
        Console.WriteLine($"COMPLETED result={result} jobsAfterThrowRan={jobAfterThrow.RunCount}")
);

var deadline = DateTime.UtcNow.AddSeconds(10);
while (runner.State != JobState.Idle)
{
    if (DateTime.UtcNow > deadline)
    {
        Console.WriteLine("TIMEOUT");
        return 2;
    }
    Thread.Sleep(1);
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
