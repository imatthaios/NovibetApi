using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Novibet.Application.Common.Interfaces;

namespace Novibet.Application.Common.Behaviors;

public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICacheableRequest
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

    public CachingBehavior(IMemoryCache cache, ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(request.CacheKey, out TResponse? cached))
        {
            _logger.LogInformation("Cache hit for {CacheKey}", request.CacheKey);
            return cached!;
        }

        _logger.LogInformation("Cache miss for {CacheKey}", request.CacheKey);
        var response = await next();
        _cache.Set(request.CacheKey, response, request.SlidingExpiration ?? TimeSpan.FromMinutes(5));

        return response;
    }
}