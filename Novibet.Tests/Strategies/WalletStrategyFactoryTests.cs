using Novibet.Application.Wallets.Strategies;

namespace Novibet.Tests.Strategies;

public class WalletStrategyFactoryTests
{
    private readonly WalletStrategyFactory _factory;

    public WalletStrategyFactoryTests()
    {
        var strategies = new IWalletStrategy[]
        {
            new AddFundsStrategy(),
            new SubtractFundsStrategy(),
            new ForceSubtractFundsStrategy()
        };
        _factory = new WalletStrategyFactory(strategies);
    }

    [Theory]
    [InlineData("add", typeof(AddFundsStrategy))]
    [InlineData("subtract", typeof(SubtractFundsStrategy))]
    [InlineData("forceSubtract", typeof(ForceSubtractFundsStrategy))]
    public void GetStrategy_ShouldReturnCorrectImplementation(string name, Type expectedType)
    {
        var strategy = _factory.GetStrategy(name);
        Assert.IsType(expectedType, strategy);
    }

    [Fact]
    public void GetStrategy_ShouldBeCaseInsensitive()
    {
        var strategy = _factory.GetStrategy("ADD");
        Assert.IsType<AddFundsStrategy>(strategy);
    }

    [Fact]
    public void GetStrategy_ShouldThrow_WhenNotFound()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => _factory.GetStrategy("unknown"));
        Assert.Equal("Strategy 'unknown' not found.", ex.Message);
    }
}