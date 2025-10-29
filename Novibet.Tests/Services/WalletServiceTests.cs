using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Novibet.Application.Wallets.Services;
using Novibet.Application.Wallets.Strategies;
using Novibet.Domain.Entities;
using Novibet.Infrastructure.Persistence;

namespace Novibet.Tests.Services
{
    public class WalletServiceTests
    {
        private readonly AppDbContext _context;
        private readonly Mock<IWalletStrategyFactory> _strategyFactoryMock = new();
        private readonly Mock<ILogger<WalletService>> _loggerMock = new();
        private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
        private readonly WalletService _walletService;

        public WalletServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _walletService = new WalletService(_context, _strategyFactoryMock.Object, _cache, _loggerMock.Object);
        }

        [Fact]
        public async Task CreateWallet_Should_Create_Successfully()
        {
            var result = await _walletService.CreateWalletAsync(100, "EUR", CancellationToken.None);

            Assert.True(result.IsSuccess);
            var wallet = await _context.Wallets.FirstOrDefaultAsync();
            Assert.NotNull(wallet);
            Assert.Equal(100, wallet.Balance);
            Assert.Equal("EUR", wallet.Currency);
        }

        [Fact]
        public async Task CreateWallet_Should_Fail_When_Negative_Balance()
        {
            var result = await _walletService.CreateWalletAsync(-50, "EUR", CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Contains("cannot be negative", result.Error);
        }

        [Fact]
        public async Task GetWallet_Should_Return_Wallet()
        {
            var wallet = new Wallet { Balance = 150, Currency = "EUR" };
            _context.Wallets.Add(wallet);
            await _context.SaveChangesAsync();

            var result = await _walletService.GetWalletAsync(wallet.Id, null, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(150, result.Data.Balance);
        }

        [Fact]
        public async Task GetWallet_Should_Fail_When_NotFound()
        {
            var result = await _walletService.GetWalletAsync(999, null, CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Contains("not found", result.Error);
        }

        [Fact]
        public async Task AdjustBalance_Should_Apply_Strategy_And_Save()
        {
            var wallet = new Wallet { Balance = 100, Currency = "EUR" };
            _context.Wallets.Add(wallet);
            await _context.SaveChangesAsync();

            var strategy = new Mock<IWalletStrategy>();
            strategy.Setup(s => s.Execute(It.IsAny<Wallet>(), 50))
                .Callback<Wallet, decimal>((w, amt) => w.Balance += amt);

            _strategyFactoryMock.Setup(f => f.GetStrategy("add")).Returns(strategy.Object);

            var result = await _walletService.AdjustBalanceAsync(wallet.Id, 50, "EUR", "add", CancellationToken.None);

            Assert.True(result.IsSuccess);
            var updated = await _context.Wallets.FirstAsync();
            Assert.Equal(150, updated.Balance);
        }

        [Fact]
        public async Task AdjustBalance_Should_Fail_When_Invalid_Amount()
        {
            var wallet = new Wallet { Balance = 100, Currency = "EUR" };
            _context.Wallets.Add(wallet);
            await _context.SaveChangesAsync();

            var result = await _walletService.AdjustBalanceAsync(wallet.Id, 0, "EUR", "add", CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Contains("must be positive", result.Error);
        }

        [Fact]
        public async Task AdjustBalance_Should_Fail_When_Wallet_NotFound()
        {
            var result = await _walletService.AdjustBalanceAsync(99, 10, "EUR", "add", CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Contains("not found", result.Error);
        }

        [Fact]
        public async Task AdjustBalance_Should_Handle_Concurrent_Requests_Safely()
        {
            var wallet = new Wallet { Id = 1, Balance = 100, Currency = "EUR" };
            _context.Wallets.Add(wallet);
            await _context.SaveChangesAsync();

            var strat = new Mock<IWalletStrategy>();
            strat.Setup(s => s.Execute(It.IsAny<Wallet>(), It.IsAny<decimal>()))
                 .Callback<Wallet, decimal>((w, amt) => w.Balance += amt);
            _strategyFactoryMock.Setup(f => f.GetStrategy("add")).Returns(strat.Object);

            var tasks = Enumerable.Range(0, 10)
                .Select(_ => _walletService.AdjustBalanceAsync(wallet.Id, 10, "EUR", "add", CancellationToken.None))
                .ToArray();

            await Task.WhenAll(tasks);

            var updated = await _context.Wallets.FirstAsync();
            Assert.Equal(200, updated.Balance); // ✅ concurrency-safe
        }

        [Fact]
        public async Task AdjustBalance_Should_Fail_When_Insufficient_Funds()
        {
            var wallet = new Wallet { Balance = 20, Currency = "EUR" };
            _context.Wallets.Add(wallet);
            await _context.SaveChangesAsync();

            var strat = new Mock<IWalletStrategy>();
            strat.Setup(s => s.Execute(It.IsAny<Wallet>(), It.IsAny<decimal>()))
                .Throws(new InvalidOperationException("Insufficient funds."));
            _strategyFactoryMock.Setup(f => f.GetStrategy("subtract")).Returns(strat.Object);

            var result = await _walletService.AdjustBalanceAsync(wallet.Id, 50, "EUR", "subtract", CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Contains("Insufficient funds", result.Error);
        }

        [Fact]
        public async Task GetWallet_Should_Convert_Currency_When_Different()
        {
            // Arrange
            var wallet = new Wallet { Balance = 100, Currency = "EUR" };
            _context.Wallets.Add(wallet);
            _context.CurrencyRates.AddRange(
                new CurrencyRate { Currency = "USD", Rate = 1.1m, Date = DateTime.UtcNow },
                new CurrencyRate { Currency = "EUR", Rate = 1.0m, Date = DateTime.UtcNow }
            );
            await _context.SaveChangesAsync();

            // Act
            var result = await _walletService.GetWalletAsync(wallet.Id, "USD", CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal("USD", result.Data!.Currency);
            Assert.Equal(110, result.Data.Balance);
        }

        [Fact]
        public async Task GetWallet_Should_Fail_When_Conversion_Rate_Missing()
        {
            var wallet = new Wallet { Balance = 100, Currency = "EUR" };
            _context.Wallets.Add(wallet);
            await _context.SaveChangesAsync();

            var result = await _walletService.GetWalletAsync(wallet.Id, "GBP", CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Contains("Conversion rate(s) missing for", result.Error);
        }
    }
}
