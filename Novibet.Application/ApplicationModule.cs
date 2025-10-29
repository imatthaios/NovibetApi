using Autofac;
using MediatR;

namespace Novibet.Application;

public class ApplicationModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterAssemblyTypes(ThisAssembly)
            .AsClosedTypesOf(typeof(IRequestHandler<,>))
            .AsImplementedInterfaces()
            .InstancePerDependency();
    }
}