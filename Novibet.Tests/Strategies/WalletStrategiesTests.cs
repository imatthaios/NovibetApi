using Novibet.Application.Wallets.Strategies;
using Novibet.Domain.Entities;

namespace Novibet.Tests.Strategies;

public class WalletStrategiesTests
{
    [Fact]
    public void AddFundsStrategy_ShouldIncreaseBalance()
    {
        // Arrange
        var wallet = new Wallet { Balance = 100m, Currency = "EUR" };
        var strategy = new AddFundsStrategy();

        // Act
        strategy.Execute(wallet, 50m);

        // Assert
        Assert.Equal(150m, wallet.Balance);
    }

    [Fact]
    public void SubtractFundsStrategy_ShouldDecreaseBalance_WhenSufficientFunds()
    {
        // Arrange
        var wallet = new Wallet { Balance = 100m, Currency = "EUR" };
        var strategy = new SubtractFundsStrategy();

        // Act
        strategy.Execute(wallet, 40m);

        // Assert
        Assert.Equal(60m, wallet.Balance);
    }

    [Fact]
    public void SubtractFundsStrategy_ShouldThrow_WhenInsufficientFunds()
    {
        // Arrange
        var wallet = new Wallet { Balance = 50m, Currency = "EUR" };
        var strategy = new SubtractFundsStrategy();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => strategy.Execute(wallet, 100m));
        Assert.Equal("Insufficient funds.", ex.Message);
    }

    [Fact]
    public void ForceSubtractFundsStrategy_ShouldAllowNegativeBalance()
    {
        // Arrange
        var wallet = new Wallet { Balance = 30m, Currency = "EUR" };
        var strategy = new ForceSubtractFundsStrategy();

        // Act
        strategy.Execute(wallet, 50m);

        // Assert
        Assert.Equal(-20m, wallet.Balance);
    }

    [Fact]
    public void AddFundsStrategy_ShouldSupportZeroAmount()
    {
        var wallet = new Wallet { Balance = 100m, Currency = "EUR" };
        var strategy = new AddFundsStrategy();

        strategy.Execute(wallet, 0m);

        Assert.Equal(100m, wallet.Balance);
    }

    [Fact]
    public void SubtractFundsStrategy_ShouldHandleExactBalance()
    {
        var wallet = new Wallet { Balance = 75m, Currency = "EUR" };
        var strategy = new SubtractFundsStrategy();

        strategy.Execute(wallet, 75m);

        Assert.Equal(0m, wallet.Balance);
    }
}
