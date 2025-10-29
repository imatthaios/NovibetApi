using System.Globalization;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Novibet.Application.Common.Interfaces;
using Novibet.Application.Interfaces;
using Novibet.Domain.Entities;
using Novibet.Infrastructure.Helpers;
using Novibet.Infrastructure.Options;

namespace Novibet.Infrastructure.Services.EcbGateway;

public class EcbRateService : IEcbRateService
{
    private readonly IMemoryCache _cache;
    private readonly IApplicationDbContext _context;
    private readonly ILogger<EcbRateService> _logger;
    private readonly EcbOptions _options;
    private readonly IHttpClientHelper _httpClientHelper;

    private const string RatesCacheKey = "ecb_rates_{0}";

    public EcbRateService(
        IMemoryCache cache,
        IApplicationDbContext context,
        IOptions<EcbOptions> options,
        ILogger<EcbRateService> logger,
        IHttpClientHelper httpClientHelper)
    {
        _context = context;
        _logger = logger;
        _cache = cache;
        _options = options.Value;
        _httpClientHelper = httpClientHelper;
    }

    public async Task UpdateRatesAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting ECB rates update from {Url}", _options.Url);

        try
        {
            var xmlContent = await _httpClientHelper.GetStringAsync(_options.Url, cancellationToken);
            var document = XDocument.Parse(xmlContent);

            var cube = document.Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "Cube" && e.Attribute("time") != null);

            if (cube == null)
            {
                _logger.LogWarning("ECB feed is missing expected structure.");
                return;
            }

            var date = DateTime.Parse(cube.Attribute("time")?.Value!, CultureInfo.InvariantCulture);
            var rates = cube.Elements()
                .Select(x => new CurrencyRate
                {
                    Currency = x.Attribute("currency")?.Value?.ToUpper() ?? "",
                    Rate = decimal.Parse(x.Attribute("rate")?.Value ?? "0", CultureInfo.InvariantCulture),
                    Date = date
                })
                .Where(r => !string.IsNullOrEmpty(r.Currency) && IsValidCurrencyCode(r.Currency))
                .ToList();

            if (rates.Count == 0)
            {
                _logger.LogWarning("No valid rates parsed from ECB feed.");
                return;
            }

            await UpdateRatesInDatabase(rates, date, cancellationToken);

            var cacheKey = string.Format(RatesCacheKey, date.ToString("yyyyMMdd"));
            _cache.Set(cacheKey, rates, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(_options.CacheDurationHours)
            });

            _logger.LogInformation("ECB rates successfully updated for {Date}. {Count} currencies processed.", 
                date.ToString("yyyy-MM-dd"), rates.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ECB rate update failed.");
            throw;
        }
    }

    public async Task<decimal?> GetRateAsync(string baseCurrency, string targetCurrency, DateTime date,
        CancellationToken cancellationToken)
    {
        if (!IsValidCurrencyCode(baseCurrency) || !IsValidCurrencyCode(targetCurrency))
            return null;

        var cacheKey = string.Format(RatesCacheKey, date.ToString("yyyyMMdd"));
        if (_cache.TryGetValue(cacheKey, out List<CurrencyRate>? cachedRates))
        {
            _logger.LogDebug("Cache hit for ECB rates on {Date}", date);
            return CalculateCrossRate(cachedRates, baseCurrency, targetCurrency);
        }

        _logger.LogDebug("Cache miss for ECB rates on {Date}, querying database...", date);

        var rates = await GetRatesFromDatabase(date, cancellationToken);
        if (rates.Count == 0)
        {
            _logger.LogWarning("No rates found in database for {Date}", date);
            return null;
        }
        _cache.Set(cacheKey, rates, TimeSpan.FromHours(_options.CacheDurationHours));
        
        return CalculateCrossRate(rates, baseCurrency, targetCurrency);
    }

    private async Task<List<CurrencyRate>> GetRatesFromDatabase(DateTime date, CancellationToken cancellationToken)
    {
        try
        {
            return await _context.CurrencyRates
                .Where(r => r.Date.Date == date.Date)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database read error while getting rates for {Date}", date);
            return new List<CurrencyRate>();
        }
    }

    private async Task UpdateRatesInDatabase(List<CurrencyRate> rates, DateTime date, CancellationToken cancellationToken)
    {
        try
        {
            var values = string.Join(", ",
                rates.Select(r =>
                    $"('{r.Currency}', {r.Rate.ToString(CultureInfo.InvariantCulture)}, '{date:yyyy-MM-dd}'::timestamp with time zone)"));

            var sql = $@"
                MERGE INTO ""CurrencyRates"" AS target
                USING (VALUES {values})
                    AS source(""Currency"", ""Rate"", ""Date"")
                ON target.""Currency"" = source.""Currency"" AND target.""Date"" = source.""Date""
                WHEN MATCHED THEN
                    UPDATE SET ""Rate"" = source.""Rate""
                WHEN NOT MATCHED THEN
                    INSERT (""Currency"", ""Rate"", ""Date"")
                    VALUES (source.""Currency"", source.""Rate"", source.""Date"");";

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            await _context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Rates merged successfully into DB for {Date}.", date.ToString("yyyy-MM-dd"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database update failed for ECB rates on {Date}", date.ToString("yyyy-MM-dd"));
        }
    }

    private static bool IsValidCurrencyCode(string code) =>
        !string.IsNullOrEmpty(code) && code.Length == 3 && code.All(char.IsLetter);

    private static decimal? CalculateCrossRate(List<CurrencyRate> rates, string baseCurrency, string targetCurrency)
    {
        var baseRate = rates.FirstOrDefault(r => r.Currency == baseCurrency)?.Rate;
        var targetRate = rates.FirstOrDefault(r => r.Currency == targetCurrency)?.Rate;

        if (baseCurrency == "EUR") return targetRate;
        if (targetCurrency == "EUR") return baseRate == null ? null : 1 / baseRate;
        if (baseRate == null || targetRate == null) return null;

        return targetRate / baseRate;
    }
}
