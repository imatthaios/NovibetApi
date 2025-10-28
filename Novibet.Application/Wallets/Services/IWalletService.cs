using Novibet.Application.Common.Models;
using Novibet.Domain.Entities;

namespace Novibet.Application.Wallets.Services;

public interface IWalletService
{
    Task<Result<long>> CreateWalletAsync(decimal initialBalance, string currency, CancellationToken cancellationToken);
    Task<Result<Wallet>> GetWalletAsync(long walletId, string? currency, CancellationToken cancellationToken);
    Task<Result> AdjustBalanceAsync(long walletId, decimal amount, string currency, string strategy, CancellationToken cancellationToken);
}