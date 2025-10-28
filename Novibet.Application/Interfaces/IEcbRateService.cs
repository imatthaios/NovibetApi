using Novibet.Application.Common.Models;
using Novibet.Domain.Entities;

namespace Novibet.Application.Interfaces;

public interface IEcbRateService
{
    Task<Result<List<CurrencyRate>>> FetchRatesAsync(CancellationToken cancellationToken);
}