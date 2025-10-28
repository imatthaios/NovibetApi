using MediatR;
using Novibet.Application.Common.Interfaces;
using Novibet.Application.Common.Models;
using Novibet.Application.Dtos;

namespace Novibet.Application.Wallets.Queries;

public class GetWalletByIdQuery : IRequest<Result<WalletDto>>, ICacheableRequest
{
    public long WalletId { get; }
    public string Currency { get; }
    
    public GetWalletByIdQuery(long walletId, string currency)
    {
        WalletId = walletId;
        Currency = currency;
    }

    public string CacheKey => $"wallet_{WalletId}_{Currency}";
    public TimeSpan? SlidingExpiration => TimeSpan.FromMinutes(10);
}
