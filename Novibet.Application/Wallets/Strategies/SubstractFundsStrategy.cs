using Novibet.Domain.Entities;

namespace Novibet.Application.Wallets.Strategies;

public class SubtractFundsStrategy : IWalletStrategy
{
    public string Name => "subtract";
    public void Execute(Wallet wallet, decimal amount)
    {
        if (wallet.Balance < amount)
            throw new InvalidOperationException("Insufficient funds.");
        wallet.Balance -= amount;
    }
}