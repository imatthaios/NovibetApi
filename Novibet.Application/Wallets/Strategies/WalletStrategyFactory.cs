namespace Novibet.Application.Wallets.Strategies;

public class WalletStrategyFactory : IWalletStrategyFactory
{
    private readonly IEnumerable<IWalletStrategy> _strategies;

    public WalletStrategyFactory(IEnumerable<IWalletStrategy> strategies)
    {
        _strategies = strategies;
    }

    public IWalletStrategy GetStrategy(string name)
    {
        var strategy = _strategies.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (strategy == null)
            throw new InvalidOperationException($"Strategy '{name}' not found.");
        return strategy;
    }
}