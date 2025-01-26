using System.Diagnostics;

namespace StarBreaker.Common;

public struct TimeLogger
{
    private long timestamp;

    public TimeLogger()
    {
        timestamp = Stopwatch.GetTimestamp();
    }

    public void LogReset(string message)
    {
        var elapsed = Stopwatch.GetElapsedTime(timestamp);
        Console.WriteLine($"{message}: {elapsed.TotalMilliseconds}ms");
        timestamp = Stopwatch.GetTimestamp();
    }
    
    public void LogContinue(string message)
    {
        var elapsed = Stopwatch.GetElapsedTime(timestamp);
        Console.WriteLine($"{message}: {elapsed.TotalMilliseconds}ms");
    }
}