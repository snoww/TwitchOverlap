using System.Diagnostics;

namespace DailyAggregation;

public static class Program
{
    public static async Task Main()
    {
        var sw = new Stopwatch();
        Console.WriteLine($"beginning daily overlap calculation at {DateTime.UtcNow:u}");
        using var agg = new Aggregate();
        await agg.BeginAggregation();
        Console.WriteLine($"total time taken: {sw.Elapsed:mm\\:ss}");
    }
}