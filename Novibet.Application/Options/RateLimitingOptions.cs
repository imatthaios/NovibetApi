namespace Novibet.Application.Options;

public class RateLimitingOptions
{
    public int RequestsPerMinute { get; set; } = 60;
}