using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Novibet.Application.Common.Interfaces;
using Novibet.Domain.Entities;

namespace Novibet.Infrastructure.Persistence;

public class AppDbContext : DbContext, IApplicationDbContext
{
    public DbConnection CreateDbConnection() => Database.GetDbConnection();
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public new DatabaseFacade Database => base.Database;
    public DbSet<CurrencyRate> CurrencyRates => Set<CurrencyRate>();
    public DbSet<Wallet> Wallets { get; set; }    
    public new Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        => base.SaveChangesAsync(cancellationToken);
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CurrencyRate>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Currency).IsRequired().HasMaxLength(3);
            e.HasIndex(x => new { x.Currency, x.Date }).IsUnique();
        });

        modelBuilder.Entity<Wallet>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Currency).IsRequired().HasMaxLength(3);
        });
    }
}