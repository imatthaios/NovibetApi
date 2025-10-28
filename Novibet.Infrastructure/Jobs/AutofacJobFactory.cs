using Autofac;
using Quartz;
using Quartz.Spi;

namespace Novibet.Infrastructure.Jobs;

public class AutofacJobFactory : IJobFactory
{
    private readonly ILifetimeScope _lifetimeScope;

    public AutofacJobFactory(ILifetimeScope lifetimeScope)
    {
        _lifetimeScope = lifetimeScope;
    }

    public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
    {
        return (IJob)_lifetimeScope.Resolve(bundle.JobDetail.JobType);
    }

    public void ReturnJob(IJob job) { }
}