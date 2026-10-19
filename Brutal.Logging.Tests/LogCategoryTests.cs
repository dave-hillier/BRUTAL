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

    // Kept in this class so xUnit never runs it in parallel with the test above; Initialize mutates global state.
    [Fact]
    public void Initialize_ConcurrentlyWithMake_GivesEveryCategoryALogger()
    {
        const int rounds = 50;
        const int makers = 8;
        const int maxPerMaker = 1000;
        var prefix = Guid.NewGuid().ToString("N");

        for (var round = 0; round < rounds; round++)
        {
            using var start = new Barrier(makers + 1);
            var initialized = false;
            Exception? initFailure = null;
            var made = new List<string>[makers];
            // Not Parallel.Invoke: it may run delegates inline on one thread and deadlock the barrier.
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
                    // Bounded so Initialize is not starved of the lock.
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
