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
}
