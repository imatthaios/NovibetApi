namespace Novibet.Infrastructure.Dtos;

public class EcbRateResponse
{
    public string Base { get; set; } = string.Empty;
    public Dictionary<string, decimal> Rates { get; set; } = new();
}