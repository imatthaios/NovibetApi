// AppDbContext.cs
using Microsoft.EntityFrameworkCore;
using Novibet.Application.Common.Interfaces;
using Novibet.Domain.Entities;
using System.Data.Common;

namespace Novibet.Infrastructure.Persistence;

public class AppDbContext : DbContext, IApplicationDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<CurrencyRate> CurrencyRates => Set<CurrencyRate>();

    public DbConnection CreateDbConnection() => Database.GetDbConnection();
    
    public async Task<int> ExecuteSqlRawAsync(string sql, object[] objects, CancellationToken cancellationToken)
    {
        return await Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }
}