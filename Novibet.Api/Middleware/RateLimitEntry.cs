namespace Novibet.Api.Middleware;

public class RateLimitEntry
{
    public int Count { get; set; }
    public DateTime PeriodStart { get; set; }
}