using Autofac;
using Autofac.Extensions.DependencyInjection;
using Novibet.Application;
using Novibet.Infrastructure;
using Novibet.Infrastructure.Jobs;
using Novibet.Infrastructure.Options;
using Quartz;

var builder = WebApplication.CreateBuilder(args);

// Use Autofac as the DI container
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

// ASP.NET Core services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();
builder.Services.AddLogging();

// MediatR
builder.Services.AddMediatR(cfg => 
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
});

builder.Services.Configure<EcbOptions>(builder.Configuration.GetSection("EcbOptions"));
builder.Services.AddQuartz(q =>
{
    var jobKey = new JobKey("EcbRateUpdateJob");
    q.AddJob<EcbRateUpdateJob>(opts => opts.WithIdentity(jobKey));

    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("EcbRateUpdateJob-trigger")
        .StartNow()
        .WithSimpleSchedule(x => 
            x.WithIntervalInMinutes(builder.Configuration.GetValue<int>("Quartz:IntervalMinutes", 1))
                .RepeatForever()));
});
builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

// Autofac Container
builder.Host.ConfigureContainer<ContainerBuilder>(container =>
{
    container.RegisterModule(new ApplicationModule());
    container.RegisterModule(new InfrastructureModule(builder.Configuration));
});

var app = builder.Build();

// Global Exception Handling
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();