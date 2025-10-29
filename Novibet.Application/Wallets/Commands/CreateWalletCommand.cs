using System.ComponentModel.DataAnnotations;
using MediatR;
using Novibet.Application.Common.Models;
using Novibet.Application.Common.Validation;

namespace Novibet.Application.Wallets.Commands;

public record CreateWalletCommand(
    [Required(ErrorMessage = "Currency code is required")]
    [ValidCurrency(ErrorMessage = "Currency code is not supported")]
    string Currency,
    [ValidDecimal(FieldName = "InitialBalance", ErrorMessage = "Initial balance must be positive")]
    decimal InitialBalance) : IRequest<Result<long>>;