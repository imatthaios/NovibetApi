// IApplicationDbContext.cs

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Novibet.Domain.Entities;

namespace Novibet.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Wallet> Wallets { get; }
    DbSet<CurrencyRate> CurrencyRates { get; }
    DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}