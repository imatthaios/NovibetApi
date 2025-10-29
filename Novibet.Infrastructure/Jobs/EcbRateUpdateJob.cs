using Microsoft.Extensions.Logging;
using Quartz;
using Novibet.Application.Interfaces;

namespace Novibet.Infrastructure.Jobs;

public class EcbRateUpdateJob : IJob
{
    private readonly IEcbRateService _ecbRateService;
    private readonly ILogger<EcbRateUpdateJob> _logger;

    public EcbRateUpdateJob(
        IEcbRateService ecbRateService,
        ILogger<EcbRateUpdateJob> logger)
    {
        _ecbRateService = ecbRateService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            _logger.LogInformation("Starting ECB rate update job");
            await _ecbRateService.UpdateRatesAsync(context.CancellationToken);
            _logger.LogInformation("ECB rate update job completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ECB rate update job failed");
            throw new JobExecutionException(ex, false);
        }
    }
}