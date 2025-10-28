using MediatR;
using Microsoft.Extensions.Logging;
using Novibet.Application.Common.Models;

namespace Novibet.Application.Common.Behaviors;

public class ResultBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private readonly ILogger<ResultBehavior<TRequest, TResponse>> _logger;

    public ResultBehavior(ILogger<ResultBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in {Request}", typeof(TRequest).Name);

            if (typeof(TResponse).IsGenericType &&
                typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
            {
                var failMethod = typeof(Result<>)
                    .MakeGenericType(typeof(TResponse).GenericTypeArguments[0])
                    .GetMethod(nameof(Result.Fail))!;

                return (TResponse)failMethod.Invoke(null, [ex.Message])!;
            }

            return (TResponse)(object)Result.Fail(ex.Message);
        }
    }
}