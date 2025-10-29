using Autofac;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Novibet.Application.Common.Interfaces;
using Novibet.Application.Interfaces;
using Novibet.Application.Wallets.Services;
using Novibet.Application.Wallets.Strategies;
using Novibet.Infrastructure.Helpers;
using Novibet.Infrastructure.Jobs;
using Novibet.Infrastructure.Options;
using Novibet.Infrastructure.Persistence;
using Novibet.Infrastructure.Services.EcbGateway;

namespace Novibet.Infrastructure;

/// <summary>
/// Autofac module for infrastructure-level dependencies:
/// - EF Core DbContext
/// - Options, HttpClient helpers
/// - ECB Rate Service
/// - Wallet strategies and services
/// - Quartz jobs
/// </summary>
public class InfrastructureModule : Module
{
    private readonly IConfiguration _configuration;

    public InfrastructureModule(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    protected override void Load(ContainerBuilder builder)
    {
        // ------------------- Database -------------------
        builder.Register(ctx =>
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseNpgsql(_configuration.GetConnectionString("DefaultConnection"));
            return new AppDbContext(optionsBuilder.Options);
        })
        .As<IApplicationDbContext>()
        .InstancePerLifetimeScope();

        // ------------------- Options -------------------
        builder.Register(ctx =>
        {
            var options = new EcbOptions();
            _configuration.GetSection("EcbOptions").Bind(options);

            options.TimeoutSeconds = options.TimeoutSeconds == 0 ? 30 : options.TimeoutSeconds;
            options.CacheDurationHours = options.CacheDurationHours == 0 ? 4 : options.CacheDurationHours;

            return options;
        }).As<EcbOptions>().SingleInstance();

        // ------------------- HTTP Helpers -------------------
        builder.RegisterType<HttpClientHelper>()
            .As<IHttpClientHelper>()
            .InstancePerLifetimeScope();

        // ------------------- ECB Rate Service -------------------
        builder.RegisterType<EcbRateService>()
            .As<IEcbRateService>()
            .InstancePerLifetimeScope();

        // ------------------- Wallet Strategies -------------------
        builder.RegisterType<AddFundsStrategy>().As<IWalletStrategy>().InstancePerDependency();
        builder.RegisterType<SubtractFundsStrategy>().As<IWalletStrategy>().InstancePerDependency();
        builder.RegisterType<ForceSubtractFundsStrategy>().As<IWalletStrategy>().InstancePerDependency();

        builder.RegisterType<WalletStrategyFactory>()
            .As<IWalletStrategyFactory>()
            .UsingConstructor(typeof(IEnumerable<IWalletStrategy>))
            .InstancePerLifetimeScope();

        builder.RegisterType<WalletService>()
            .As<IWalletService>()
            .InstancePerLifetimeScope();

        // ------------------- Quartz Jobs -------------------
        builder.RegisterType<EcbRateUpdateJob>()
            .AsSelf()
            .InstancePerDependency();
    }
}
