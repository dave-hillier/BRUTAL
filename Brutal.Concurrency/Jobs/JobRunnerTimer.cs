using System.Diagnostics;
using Brutal.Concurrency.Statistics;

namespace Brutal.Concurrency.Jobs;

/// <summary>
/// A class that can take a series of time measurements and record return some basic stats.
/// </summary>
internal class JobRunnerTimer
{
    public void Start()
    {
        _workerStopwatch.Restart();
    }

    /// <summary>Stops the current measurement and returns its duration without recording it.</summary>
    public double Stop()
    {
        _workerStopwatch.Stop();
        return _workerStopwatch.Elapsed.TotalMilliseconds;
    }

    public void Commit()
    {
        AddSample(_workerStopwatch.Elapsed.TotalMilliseconds);
    }

    public void ResetTimings()
    {
        _sampler.Reset();
    }

    public double AverageMillis => _sampler.Average;
    public double MaxMillis => _sampler.Max;
    public double LastMillis => _sampler.Last;

    #region Private

    private void AddSample(double milliSeconds)
    {
        _sampler.Add(milliSeconds);
    }

    private readonly Stopwatch _workerStopwatch = new();
    private readonly Sampler<double> _sampler = new();

    #endregion
}
