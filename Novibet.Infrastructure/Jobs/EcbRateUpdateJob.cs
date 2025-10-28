using System.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Novibet.Application.Common.Interfaces;
using Novibet.Infrastructure.Services.EcbGateway;
using Quartz;

namespace Novibet.Infrastructure.Jobs;

[DisallowConcurrentExecution]
public class EcbRateUpdateJob : IJob
{
    private readonly EcbRateService _ecbService;
    private readonly IApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<EcbRateUpdateJob> _logger;

    public EcbRateUpdateJob(
        EcbRateService ecbService,
        IApplicationDbContext context,
        IMemoryCache cache,
        ILogger<EcbRateUpdateJob> logger)
    {
        _ecbService = ecbService;
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Running ECB rate update job at {Time}", DateTime.UtcNow);

        var result = await _ecbService.FetchRatesAsync(context.CancellationToken);
        if (!result.IsSuccess)
        {
            _logger.LogError("ECB job aborted: {Error}", result.Error);
            return;
        }

        var rates = result.Data;
        if (rates is null || rates.Count == 0)
        {
            _logger.LogWarning("ECB job found no rates to update.");
            return;
        }

        await using var dbConn = _context.CreateDbConnection();
        await dbConn.OpenAsync(context.CancellationToken);
        await using var transaction = await dbConn.BeginTransactionAsync(context.CancellationToken);

        try
        {
            // Parameterized MERGE (PostgreSQL upsert via ON CONFLICT)
            var command = dbConn.CreateCommand();
            command.Transaction = transaction;

            // Use temp table style VALUES list
            var valuesSql = string.Join(",",
                rates.Select((r, i) =>
                    $"(@currency{i}, @rate{i}, @date{i}::date)"));

            command.CommandText = $@"
                INSERT INTO ""CurrencyRates"" (""Currency"", ""Rate"", ""Date"")
                VALUES {valuesSql}
                ON CONFLICT (""Currency"", ""Date"")
                DO UPDATE SET ""Rate"" = EXCLUDED.""Rate"";";

            // Add parameters safely
            for (int i = 0; i < rates.Count; i++)
            {
                var rate = rates[i];
                var p1 = command.CreateParameter();
                p1.ParameterName = $"@currency{i}";
                p1.Value = rate.Currency;
                p1.DbType = DbType.String;
                command.Parameters.Add(p1);

                var p2 = command.CreateParameter();
                p2.ParameterName = $"@rate{i}";
                p2.Value = rate.Rate;
                p2.DbType = DbType.Decimal;
                command.Parameters.Add(p2);

                var p3 = command.CreateParameter();
                p3.ParameterName = $"@date{i}";
                p3.Value = rate.Date.Date;
                p3.DbType = DbType.Date;
                command.Parameters.Add(p3);
            }

            await command.ExecuteNonQueryAsync(context.CancellationToken);
            await transaction.CommitAsync(context.CancellationToken);

            _logger.LogInformation("✅ ECB database updated successfully ({Count} rates).", rates.Count);

            // Refresh cache
            _cache.Set("latest_ecb_rates", rates, TimeSpan.FromMinutes(60));
            _logger.LogInformation("🧠 ECB rates cache refreshed.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(context.CancellationToken);
            _logger.LogError(ex, "❌ ECB job failed during database update.");
        }
        finally
        {
            await dbConn.CloseAsync();
        }
    }
}
