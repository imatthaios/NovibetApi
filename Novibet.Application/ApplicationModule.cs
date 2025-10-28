using Autofac;
using MediatR;
using Novibet.Application.Wallets.Services;
using Novibet.Application.Wallets.Strategies;

namespace Novibet.Application;

public class ApplicationModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Register WalletService
        builder.RegisterType<WalletService>()
            .As<IWalletService>()
            .InstancePerLifetimeScope();
        
        builder.RegisterType<AddFundsStrategy>().As<IWalletStrategy>();
        builder.RegisterType<SubtractFundsStrategy>().As<IWalletStrategy>();
        builder.RegisterType<ForceSubtractFundsStrategy>().As<IWalletStrategy>();

        // Strategies
        builder.RegisterType<WalletStrategyFactory>()
            .As<IWalletStrategyFactory>()
            .SingleInstance();

        // Register MediatR handlers from Application assembly
        builder.RegisterAssemblyTypes(ThisAssembly)
            .AsClosedTypesOf(typeof(IRequestHandler<,>))
            .AsImplementedInterfaces()
            .InstancePerDependency();
    }
}