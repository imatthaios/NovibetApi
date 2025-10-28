using Novibet.Domain.Entities;

namespace Novibet.Application.Wallets.Strategies;

public class ForceSubtractFundsStrategy : IWalletStrategy
{
    public string Name => "forcesubtract";
    public void Execute(Wallet wallet, decimal amount)
    {
        wallet.Balance -= amount;
    }
}