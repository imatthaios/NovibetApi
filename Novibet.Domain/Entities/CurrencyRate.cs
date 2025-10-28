namespace Novibet.Domain.Entities;

public class CurrencyRate
{
    public int Id { get; set; }
    public string Currency { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public DateTime Date { get; set; }
}