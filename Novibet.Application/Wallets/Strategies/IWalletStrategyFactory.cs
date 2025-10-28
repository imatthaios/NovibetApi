namespace Novibet.Application.Wallets.Strategies;

public interface IWalletStrategyFactory
{
    IWalletStrategy GetStrategy(string name);
}