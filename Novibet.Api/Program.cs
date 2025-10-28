using Autofac;
using Autofac.Extensions.DependencyInjection;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Novibet.Application;
using Novibet.Application.Common.Behaviors;
using Novibet.Application.Options;
using Novibet.Application.Wallets.Commands;
using Novibet.Infrastructure;
using Quartz;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Services.Configure<EcbApiOptions>(builder.Configuration.GetSection("EcbApi"));
builder.Services.Configure<QuartzOptions>(builder.Configuration.GetSection("Quartz"));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();
builder.Services.AddLogging();

builder.Host.ConfigureContainer<ContainerBuilder>(container =>
{
    // Memory Cache
    container.RegisterType<MemoryCache>().As<IMemoryCache>().SingleInstance();

    // Logging factory
    container.RegisterType<LoggerFactory>().As<ILoggerFactory>().SingleInstance();

    // MediatR
    container.RegisterType<Mediator>().As<IMediator>().InstancePerLifetimeScope();

    // Application + Infrastructure Modules
    container.RegisterModule(new ApplicationModule());
    container.RegisterModule(new InfrastructureModule(builder.Configuration));

    // Pipeline Behaviors
    container.RegisterGeneric(typeof(LoggingBehavior<,>)).As(typeof(IPipelineBehavior<,>)).InstancePerLifetimeScope();
    container.RegisterGeneric(typeof(CachingBehavior<,>)).As(typeof(IPipelineBehavior<,>)).InstancePerLifetimeScope();
    container.RegisterGeneric(typeof(ResultBehavior<,>)).As(typeof(IPipelineBehavior<,>)).InstancePerLifetimeScope();

    container.RegisterAssemblyTypes(typeof(AdjustWalletBalanceCommand).Assembly)
        .AsClosedTypesOf(typeof(IRequestHandler<,>))
        .AsImplementedInterfaces()
        .InstancePerLifetimeScope();

    // MediatR Handlers
    container.RegisterAssemblyTypes(typeof(AssemblyMarker).Assembly)
        .AsClosedTypesOf(typeof(IRequestHandler<,>))
        .AsImplementedInterfaces();
});

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var schedulerTask = scope.ServiceProvider.GetService<Task<IScheduler>>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("StartupScheduler");

    if (schedulerTask != null)
    {
        var scheduler = await schedulerTask;
        logger.LogInformation("✅ Quartz Scheduler started successfully.");

        // Optionally verify job registration
        var jobKeys = await scheduler.GetJobKeys(Quartz.Impl.Matchers.GroupMatcher<JobKey>.AnyGroup());
        foreach (var jobKey in jobKeys)
            logger.LogInformation("Quartz Job loaded: {JobKey}", jobKey.Name);
    }
    else
    {
        logger.LogWarning("⚠️ Quartz Scheduler not resolved. Check InfrastructureModule registration.");
    }
}

// -------------------------------------------------------
// 🌐 Global Exception Handling Middleware
// -------------------------------------------------------
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("GlobalException");
        logger.LogError(ex, "Unhandled exception occurred");

        context.Response.StatusCode = 500;
        await context.Response.WriteAsJsonAsync(new { error = "An internal server error occurred." });
    }
});

// -------------------------------------------------------
// 🚦 Middleware pipeline
// -------------------------------------------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
