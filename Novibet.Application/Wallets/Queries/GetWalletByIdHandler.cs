using MediatR;
using Novibet.Application.Common.Models;
using Novibet.Application.Wallets.Services;
using Novibet.Application.Dtos;

namespace Novibet.Application.Wallets.Queries;

public class GetWalletByIdHandler : IRequestHandler<GetWalletByIdQuery, Result<WalletDto>>
{
    private readonly IWalletService _walletService;

    public GetWalletByIdHandler(IWalletService walletService)
    {
        _walletService = walletService;
    }

    public async Task<Result<WalletDto>> Handle(GetWalletByIdQuery request, CancellationToken cancellationToken)
    {
        var result = await _walletService.GetWalletAsync(request.WalletId, request.Currency, cancellationToken);
        if (result is { IsSuccess: false, Error: not null }) return Result<WalletDto>.Fail(result.Error);

        var wallet = result.Data!;
        
        return Result<WalletDto>.Ok(new WalletDto(wallet.Id, wallet.Balance, wallet.Currency));
    }
}