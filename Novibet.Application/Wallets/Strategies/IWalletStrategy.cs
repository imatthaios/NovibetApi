using Novibet.Domain.Entities;

namespace Novibet.Application.Wallets.Strategies;

public interface IWalletStrategy
{
    void Execute(Wallet wallet, decimal amount);
    string Name { get; }
}