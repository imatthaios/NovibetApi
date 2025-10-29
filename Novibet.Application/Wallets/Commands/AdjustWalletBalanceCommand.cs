using System.ComponentModel.DataAnnotations;
using MediatR;
using Novibet.Application.Common.Models;
using Novibet.Application.Common.Validation;

namespace Novibet.Application.Wallets.Commands;

public record AdjustWalletBalanceCommand(
    [Required(ErrorMessage = "WalletId is required")]
    long WalletId,
    [ValidDecimal(FieldName = "Amount", ErrorMessage = "Initial balance must be positive")]
    decimal Amount,
    string Currency,
    [Required(ErrorMessage = "Strategy is required")]
    string Strategy) : IRequest<Result>;