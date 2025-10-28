using MediatR;
using Novibet.Application.Common.Models;

namespace Novibet.Application.Wallets.Commands;

public record AdjustWalletBalanceCommand(long WalletId, decimal Amount, string Currency, string Strategy)
    : IRequest<Result<bool>>;