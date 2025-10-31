using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Novibet.Application.Common.Interfaces;
using Novibet.Application.Common.Models;
using Novibet.Application.Wallets.Strategies;
using Novibet.Domain.Entities;

namespace Novibet.Application.Wallets.Services;

public class WalletService : IWalletService
{
    private readonly IApplicationDbContext _context;
    private readonly IWalletStrategyFactory _strategyFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<WalletService> _logger;
    private static readonly ConcurrentDictionary<long, SemaphoreSlim> WalletLocks = new();

    private static readonly MemoryCacheEntryOptions CacheOptions = new()
    {
        SlidingExpiration = TimeSpan.FromMinutes(5),
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
    };

    public WalletService(
        IApplicationDbContext context,
        IWalletStrategyFactory strategyFactory,
        IMemoryCache cache,
        ILogger<WalletService> logger)
    {
        _context = context;
        _strategyFactory = strategyFactory;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Result<long>> CreateWalletAsync(decimal initialBalance, string currency,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating wallet with balance {Balance} and currency {Currency}", initialBalance,
            currency);
        if (initialBalance < 0)
        {
            _logger.LogWarning("Initial balance cannot be negative: {Balance}", initialBalance);
            return Result<long>.Fail("Initial balance cannot be negative.");
        }
        var wallet = new Wallet
        {
            Balance = initialBalance,
            Currency = currency.ToUpperInvariant()
        };
        _context.Wallets.Add(wallet);

        try
        {
            _context.Wallets.Add(wallet);
            await _context.SaveChangesAsync(cancellationToken);
            _cache.Set($"wallet_{wallet.Id}_{wallet.Currency}", wallet, CacheOptions);
            _logger.LogInformation("Created wall1et {Id} {Currency}", wallet.Id, wallet.Currency);

            return Result<long>.Ok(wallet.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateWallet failed");
            return Result<long>.Fail("Database error.");
        }
    }

    public async Task<Result<Wallet>> GetWalletAsync(long id, string? currency, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching wallet {WalletId} (currency={Currency})", id, currency);

        if (_cache.TryGetValue($"wallet_{id}_{currency}", out Wallet? cachedWallet))
        {
            _logger.LogDebug("Cache hit for wallet {WalletId} and currency {Currency}", id, currency);
            if (!string.IsNullOrEmpty(currency) &&
                !currency.Equals(cachedWallet?.Currency, StringComparison.OrdinalIgnoreCase))
            {
                var conversionResult = await ConvertCurrencyAsync(cachedWallet!, currency, cancellationToken);
                if (conversionResult is { IsSuccess: false, Error: not null })
                    return Result<Wallet>.Fail(conversionResult.Error);
            }

            if (cachedWallet != null) return Result<Wallet>.Ok(cachedWallet);
        }

        _logger.LogDebug("Cache miss for wallet {WalletId} and currency {Currency}, loading from DB", id, currency);

        var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
        if (wallet == null)
        {
            _logger.LogWarning("Wallet {WalletId} not found.", id);
            return Result<Wallet>.Fail($"Wallet with ID {id} not found.");
        }

        if (!string.IsNullOrEmpty(currency) && !currency.Equals(wallet.Currency, StringComparison.OrdinalIgnoreCase))
        {
            var conversionResult = await ConvertCurrencyAsync(wallet, currency, cancellationToken);
            if (!conversionResult.IsSuccess) return Result<Wallet>.Fail(conversionResult.Error);
        }

        _cache.Set($"wallet_{id}_{currency}", wallet, CacheOptions);

        return Result<Wallet>.Ok(wallet);
    }

    public async Task<Result> AdjustBalanceAsync(long walletId, decimal amount, string currency, string strategy,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Adjusting wallet {WalletId} by {Amount} {Currency} using strategy {Strategy}", walletId,
            amount, currency, strategy);

        if (amount <= 0) return Result.Fail("Amount must be positive.");

        var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.Id == walletId, cancellationToken);
        if (wallet == null)
        {
            _logger.LogWarning("Wallet {WalletId} not found.", walletId);
            return Result.Fail($"Wallet with ID {walletId} not found.");
        }

        var walletLock = WalletLocks.GetOrAdd(walletId, _ => new SemaphoreSlim(1, 1));
        await walletLock.WaitAsync(cancellationToken);

        try
        {
            var strat = _strategyFactory.GetStrategy(strategy);
            strat.Execute(wallet, amount);

            await _context.SaveChangesAsync(cancellationToken);
            _cache.Remove($"wallet_{walletId}_{currency}");
            _cache.Set($"wallet_{walletId}_{currency}", wallet, CacheOptions);
            _logger.LogInformation("Wallet {WalletId} adjusted successfully (new balance={Balance})", walletId,
                wallet.Balance);

            return Result.Ok();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business rule violation while adjusting wallet {WalletId}", walletId);
            return Result.Fail(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error adjusting wallet {WalletId}", walletId);
            return Result.Fail($"Unexpected error: {ex.Message}");
        }
        finally
        {
            walletLock.Release();
        }
    }

    private async Task<Result> ConvertCurrencyAsync(Wallet wallet, string targetCurrency,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Converting wallet {WalletId} from {FromCurrency} to {ToCurrency}", wallet.Id, wallet.Currency,
            targetCurrency);

        if (wallet.Currency.Equals(targetCurrency, StringComparison.OrdinalIgnoreCase))
            return Result.Ok();

        var rates = await _cache.GetOrCreateAsync("currency_rates", async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(5);
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            _logger.LogDebug("Cache miss for currency rates, loading from DB");
            return await _context.CurrencyRates.OrderByDescending(r => r.Date).ToListAsync(cancellationToken);
        });

        var toRate = rates.FirstOrDefault(r => r.Currency == targetCurrency);
        var fromRate = rates.FirstOrDefault(r => r.Currency == wallet.Currency);

        if (toRate == null || (wallet.Currency != "EUR" && fromRate == null))
        {
            _logger.LogWarning("Missing conversion rate for {From}->{To}", wallet.Currency, targetCurrency);
            return Result.Fail($"Conversion rate(s) missing for {wallet.Currency} or {targetCurrency}.");
        }

        if (wallet.Currency == "EUR")
            wallet.Balance *= toRate.Rate;
        else if (targetCurrency == "EUR" && fromRate != null)
            wallet.Balance /= fromRate.Rate;
        else if (fromRate != null)
            wallet.Balance = (wallet.Balance / fromRate.Rate) * toRate.Rate;

        wallet.Currency = targetCurrency.ToUpperInvariant();

        _logger.LogInformation("Wallet {WalletId} converted successfully to {Currency} (new balance={Balance})",
            wallet.Id, targetCurrency, wallet.Balance);

        return Result.Ok();
    }
}