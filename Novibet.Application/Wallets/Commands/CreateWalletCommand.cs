using MediatR;
using Novibet.Application.Common.Models;

namespace Novibet.Application.Wallets.Commands;

public record CreateWalletCommand(string Currency, decimal InitialBalance) : IRequest<Result<long>>;