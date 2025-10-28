using MediatR;
using Novibet.Application.Common.Models;
using Novibet.Application.Wallets.Services;

namespace Novibet.Application.Wallets.Commands;

public class AdjustWalletBalanceHandler : IRequestHandler<AdjustWalletBalanceCommand, Result>
{
    private readonly IWalletService _walletService;

    public AdjustWalletBalanceHandler(IWalletService walletService)
    {
        _walletService = walletService;
    }

    public async Task<Result> Handle(AdjustWalletBalanceCommand request, CancellationToken cancellationToken)
        => await _walletService.AdjustBalanceAsync(
            request.WalletId, request.Amount, request.Currency, request.Strategy, cancellationToken);
}