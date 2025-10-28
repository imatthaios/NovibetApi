using MediatR;
using Microsoft.AspNetCore.Mvc;
using Novibet.Application.Wallets.Commands;
using Novibet.Application.Wallets.Queries;

namespace Novibet.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WalletController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<WalletController> _logger;

    public WalletController(IMediator mediator, ILogger<WalletController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CreateWallet([FromBody] CreateWalletCommand command)
    {
        try
        {
            _logger.LogInformation("Creating wallet with currency {Currency}", command.Currency);
            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Wallet creation failed: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            return Ok(new { walletId = result.Data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while creating wallet");
            return StatusCode(500, new { error = "An internal server error occurred." });
        }
    }

    [HttpGet("{walletId:long}")]
    public async Task<IActionResult> GetWallet(long walletId, [FromQuery] string? currency)
    {
        try
        {
            _logger.LogInformation("Fetching wallet {WalletId} with currency {currency}", walletId, currency);
            var result = await _mediator.Send(new GetWalletByIdQuery(walletId, currency?.ToUpper() ?? "EUR"));

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to fetch wallet {WalletId}: {Error}", walletId, result.Error);
                return NotFound(new { error = result.Error });
            }

            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching wallet {WalletId}", walletId);
            return StatusCode(500, new { error = "An internal server error occurred." });
        }
    }

    [HttpPost("{walletId:long}/adjustbalance")]
    public async Task<IActionResult> AdjustBalance(
        long walletId,
        [FromQuery] decimal amount,
        [FromQuery] string currency,
        [FromQuery] string strategy)
    {
        try
        {
            _logger.LogInformation("Adjusting wallet {WalletId} with strategy {Strategy}", walletId, strategy);
            var result = await _mediator.Send(new AdjustWalletBalanceCommand(walletId, amount, currency, strategy));

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to adjust wallet {WalletId}: {Error}", walletId, result.Error);
                return BadRequest(new { error = result.Error });
            }

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while adjusting wallet {WalletId}", walletId);
            return StatusCode(500, new { error = "An internal server error occurred." });
        }
    }
}
