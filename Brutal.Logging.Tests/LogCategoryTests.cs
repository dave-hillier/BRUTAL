namespace Brutal.Logging.Tests;

public class LogCategoryTests
{
    [Fact]
    public void Make_FromManyThreadsConcurrently_RegistersEveryCategory()
    {
        const int count = 2000;
        var prefix = Guid.NewGuid().ToString("N");
        Exception? failure = null;
        try
        {
            Parallel.For(0, count, new ParallelOptions { MaxDegreeOfParallelism = 16 }, i => LogCategory.Make($"{prefix}-{i}"));
        }
        catch (Exception e)
        {
            failure = e;
        }

        Assert.Null(failure);
        for (var i = 0; i < count; i++)
            Assert.True(LogCategory.TryGet($"{prefix}-{i}", out _), $"category {i} missing");
    }

    // Lives in this class (one xUnit collection, so it never runs alongside the test above) because
    // LogSystem.Initialize mutates process-wide state.
    [Fact]
    public void Initialize_ConcurrentlyWithMake_GivesEveryCategoryALogger()
    {
        const int rounds = 50;
        const int makers = 8;
        const int maxPerMaker = 1000;
        var prefix = Guid.NewGuid().ToString("N");

        for (var round = 0; round < rounds; round++)
        {
            // Every participant waits here, so Initialize and the Make calls start together.
            using var start = new Barrier(makers + 1);
            var initialized = false;
            Exception? initFailure = null;
            var made = new List<string>[makers];
            // Dedicated threads rather than Parallel.Invoke, which may run delegates inline on one
            // thread and deadlock the barrier.
            var initializer = new Thread(() =>
            {
                try
                {
                    start.SignalAndWait();
                    LogSystem.Initialize(_ => { });
                }
                catch (Exception e)
                {
                    initFailure = e;
                }
                finally
                {
                    Volatile.Write(ref initialized, true);
                }
            });
            var makerFailures = new Exception?[makers];
            var makerThreads = Enumerable.Range(0, makers).Select(m => new Thread(() =>
            {
                made[m] = [];
                try
                {
                    start.SignalAndWait();
                    // Keep making until Initialize has returned (bounded, so the registry stays small
                    // and Initialize is not starved of the lock), so the two overlap.
                    for (var i = 0; i < maxPerMaker && !Volatile.Read(ref initialized); i++)
                    {
                        var name = $"{prefix}-{round}-{m}-{i}";
                        LogCategory.Make(name);
                        made[m].Add(name);
                    }
                }
                catch (Exception e)
                {
                    makerFailures[m] = e;
                }
            })).ToArray();

            initializer.Start();
            foreach (var t in makerThreads)
                t.Start();
            initializer.Join();
            foreach (var t in makerThreads)
                t.Join();

            Assert.Null(initFailure);
            Assert.All(makerFailures, Assert.Null);
            foreach (var name in made.SelectMany(names => names))
                Assert.NotNull(LogCategory.Get(name).Logger);
        }
    }
}
