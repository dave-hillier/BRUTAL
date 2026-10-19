using Brutal.Concurrency.Statistics;

namespace Brutal.Concurrency.Tests;

public class MovingAverageTests
{
    [Fact]
    public void Average_RecoversOnceLargeSampleLeavesWindow()
    {
        var average = new MovingAverage<double>(2);
        average.Update(1e17);
        average.Update(1);
        var result = average.Update(1);

        Assert.Equal(1.0, result);
        Assert.Equal(1.0, average.CurrentAverage);
    }

    [Fact]
    public void Average_RecoversOnceInfiniteSampleLeavesWindow()
    {
        var average = new MovingAverage<double>(2);
        average.Update(double.PositiveInfinity);
        average.Update(2);
        var result = average.Update(4);

        Assert.Equal(3.0, result);
    }
}
