using Autofac;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Novibet.Application.Common.Interfaces;
using Novibet.Application.Interfaces;
using Novibet.Application.Options;
using Novibet.Infrastructure.Jobs;
using Novibet.Infrastructure.Persistence;
using Novibet.Infrastructure.Services.EcbGateway;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;

namespace Novibet.Infrastructure;

public class InfrastructureModule : Module
{
    private readonly IConfiguration _configuration;
    public InfrastructureModule(IConfiguration configuration) => _configuration = configuration;

    protected override void Load(ContainerBuilder builder)
    {
        // DbContext
        builder.Register(c =>
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseNpgsql(_configuration.GetConnectionString("DefaultConnection"));
            return new AppDbContext(optionsBuilder.Options);
        }).As<IApplicationDbContext>()
          .InstancePerLifetimeScope();

        // Memory cache
        builder.RegisterType<MemoryCache>()
               .As<IMemoryCache>()
               .SingleInstance();

        // Logger factory
        builder.RegisterType<LoggerFactory>()
               .As<ILoggerFactory>()
               .SingleInstance();

        // HttpClient
        builder.Register(c => new HttpClient { Timeout = TimeSpan.FromSeconds(30) })
               .As<HttpClient>()
               .SingleInstance();

        // Bind ECB API options
        builder.Register(c =>
        {
            var options = new EcbApiOptions();
            _configuration.GetSection("EcbApi").Bind(options);
            return Options.Create(options);
        }).As<IOptions<EcbApiOptions>>()
          .SingleInstance();

        // ECB rate service
        builder.RegisterType<EcbRateService>()
               .As<IEcbRateService>()
               .AsSelf()
               .InstancePerDependency();

        // Job factory FIRST
        builder.RegisterType<AutofacJobFactory>()
               .As<IJobFactory>()
               .SingleInstance();

        // Job registration
        builder.RegisterType<EcbRateUpdateJob>()
               .AsSelf()
               .As<IJob>()
               .InstancePerDependency();

        // Scheduler registration
        builder.Register(async c =>
        {
            var schedulerFactory = new StdSchedulerFactory();
            var scheduler = await schedulerFactory.GetScheduler();

            var jobFactory = c.Resolve<IJobFactory>();
            scheduler.JobFactory = jobFactory;

            var job = JobBuilder.Create<EcbRateUpdateJob>()
                .WithIdentity("EcbRateUpdateJob")
                .Build();

            var trigger = TriggerBuilder.Create()
                .WithIdentity("EcbRateUpdateTrigger")
                .StartNow()
                .WithSimpleSchedule(x =>
                    x.WithIntervalInMinutes(1)
                     .RepeatForever())
                .Build();

            await scheduler.ScheduleJob(job, trigger);
            await scheduler.Start();

            var logger = c.Resolve<ILoggerFactory>().CreateLogger("QuartzScheduler");
            logger.LogInformation("Quartz Scheduler started successfully and EcbRateUpdateJob scheduled.");

            return scheduler;
        })
        .As<Task<IScheduler>>()
        .SingleInstance();
    }
}
