using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Novibet.Domain.Entities;

namespace Novibet.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Wallet> Wallets { get; }
    DbSet<CurrencyRate> CurrencyRates { get; }
    DbConnection CreateDbConnection();
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}