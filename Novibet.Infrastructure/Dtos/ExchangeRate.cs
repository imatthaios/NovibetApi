namespace Novibet.Infrastructure.Dtos;

public class ExchangeRate
{
    public int Id { get; set; }
    public string Currency { get; set; } = null!;
    public decimal Rate { get; set; }
    public DateTime Date { get; set; }
}