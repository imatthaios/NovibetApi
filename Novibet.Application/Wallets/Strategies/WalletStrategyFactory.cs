namespace Novibet.Application.Wallets.Strategies;

public class WalletStrategyFactory : IWalletStrategyFactory
{
    private readonly Dictionary<string, IWalletStrategy> _strategies = new(StringComparer.OrdinalIgnoreCase);

    public WalletStrategyFactory(IEnumerable<IWalletStrategy> strategies)
    {
        foreach (var strat in strategies)
            _strategies[strat.Name] = strat;
    }

    public IWalletStrategy GetStrategy(string name)
    {
        if (_strategies.TryGetValue(name, out var strat))
            return strat;

        throw new InvalidOperationException($"Strategy '{name}' not found.");
    }
}