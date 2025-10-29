namespace Novibet.Application.Interfaces;

public interface IEcbRateService
{
    Task UpdateRatesAsync(CancellationToken cancellationToken);
}
