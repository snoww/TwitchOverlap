namespace DailyAggregation;

public static class Program
{
    public static async Task Main()
    {
        using var agg = new Aggregate();
        await agg.BeginAggregation();
    }
}