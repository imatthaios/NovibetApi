using System.Xml.Linq;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Novibet.Application.Common.Models;
using Novibet.Application.Options;
using Novibet.Application.Interfaces;
using Novibet.Domain.Entities;

namespace Novibet.Infrastructure.Services.EcbGateway;

public class EcbRateService : IEcbRateService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<EcbRateService> _logger;
    private readonly EcbApiOptions _options;

    private const string CacheKey = "latest_ecb_rates";

    public EcbRateService(
        HttpClient httpClient,
        IMemoryCache cache,
        IOptions<EcbApiOptions> options,
        ILogger<EcbRateService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<Result<List<CurrencyRate>>> FetchRatesAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_cache.TryGetValue(CacheKey, out List<CurrencyRate>? cachedRates))
            {
                if (cachedRates != null)
                {
                    _logger.LogInformation("Returning cached ECB rates ({Count} entries).", cachedRates.Count);
                    return Result<List<CurrencyRate>>.Ok(cachedRates);
                }
            }

            _logger.LogInformation("Fetching ECB rates from {Url}", _options.Url);

            var response = await _httpClient.GetStringAsync(_options.Url, cancellationToken);
            var xml = XDocument.Parse(response);

            var cube = xml.Descendants().FirstOrDefault(e => e.Attribute("time") != null);
            if (cube == null)
            {
                _logger.LogWarning("ECB XML response did not contain expected <Cube> structure.");
                return Result<List<CurrencyRate>>.Fail("Invalid ECB data format.");
            }

            var date = DateTime.Parse(cube.Attribute("time")!.Value);
            var rates = cube.Elements()
                .Select(e => new CurrencyRate
                {
                    Currency = e.Attribute("currency")!.Value,
                    Rate = decimal.Parse(e.Attribute("rate")!.Value),
                    Date = date
                })
                .ToList();

            rates.Add(new CurrencyRate { Currency = "EUR", Rate = 1m, Date = date });
            _cache.Set(CacheKey, rates, TimeSpan.FromHours(1));
            _logger.LogInformation("Fetched and cached {Count} ECB rates for {Date}.", rates.Count, date);

            return Result<List<CurrencyRate>>.Ok(rates);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error while fetching ECB rates.");
            return Result<List<CurrencyRate>>.Fail("Failed to fetch rates due to network error.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching ECB rates.");
            return Result<List<CurrencyRate>>.Fail("Unexpected error during ECB fetch.");
        }
    }
}
