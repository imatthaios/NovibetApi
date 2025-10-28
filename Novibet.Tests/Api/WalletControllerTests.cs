using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Novibet.Api.Controllers;
using Novibet.Application.Common.Models;
using Novibet.Application.Dtos;
using Novibet.Application.Wallets.Commands;
using Novibet.Application.Wallets.Queries;

namespace Novibet.Tests.Api;

public class WalletsControllerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<ILogger<WalletController>> _loggerMock = new();
    private readonly WalletController _controller;

    public WalletsControllerTests()
    {
        _controller = new WalletController(_mediatorMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task CreateWallet_Should_ReturnOk_When_Success()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<CreateWalletCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<long>.Ok(1));

        var result = await _controller.CreateWallet(new CreateWalletCommand("EUR", 100)) as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public async Task GetWallet_Should_ReturnOk_When_Found()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetWalletByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<WalletDto>.Ok(new WalletDto(1, 100, "EUR" )));

        var result = await _controller.GetWallet(1, "EUR") as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public async Task AdjustBalance_Should_ReturnOk_When_Success()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<AdjustWalletBalanceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Ok(true));

        var result = await _controller.AdjustBalance(1, 50, "EUR", "add") as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }
}