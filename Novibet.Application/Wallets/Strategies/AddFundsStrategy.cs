using Novibet.Domain.Entities;

namespace Novibet.Application.Wallets.Strategies;

public class AddFundsStrategy : IWalletStrategy
{
    public string Name => "add";
    public void Execute(Wallet wallet, decimal amount)
    {
        wallet.Balance += amount;
    }
}