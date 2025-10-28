using MediatR;
using Microsoft.Extensions.Logging;
using Novibet.Application.Common.Models;
using Novibet.Application.Wallets.Services;

namespace Novibet.Application.Wallets.Commands;

public class CreateWalletHandler : IRequestHandler<CreateWalletCommand, Result<long>>
{
    private readonly IWalletService _walletService;
    private readonly ILogger<CreateWalletHandler> _logger;

    public CreateWalletHandler(
        IWalletService walletService,
        ILogger<CreateWalletHandler> logger)
    {
        _walletService = walletService;
        _logger = logger;
    }

    public async Task<Result<long>> Handle(CreateWalletCommand request, CancellationToken cancellationToken)
        => await _walletService.CreateWalletAsync(request.InitialBalance, request.Currency, cancellationToken);
}