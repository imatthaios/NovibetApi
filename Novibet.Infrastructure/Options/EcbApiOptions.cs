namespace Novibet.Infrastructure.Options;

public class EcbOptions
{
    public string Url { get; set; } = "https://www.ecb.europa.eu/stats/eurofxref/eurofxref-daily.xml";
    public int TimeoutSeconds { get; set; } = 30;
    public int CacheDurationHours { get; set; } = 4;
    public int RetryCount { get; set; } = 3;
}