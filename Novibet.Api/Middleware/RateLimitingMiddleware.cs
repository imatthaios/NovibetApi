using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Novibet.Application.Options;

namespace Novibet.Api.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly RateLimitingOptions _options;
    private static readonly ConcurrentDictionary<string, RateLimitEntry> RateLimits = new();

    public RateLimitingMiddleware(
        RequestDelegate next,
        IOptions<RateLimitingOptions> options,
        ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        var entry = RateLimits.GetOrAdd(ip, _ => new RateLimitEntry
        {
            Count = 0,
            PeriodStart = DateTime.UtcNow
        });

        lock (entry)
        {
            var now = DateTime.UtcNow;
            if ((now - entry.PeriodStart).TotalMinutes >= 1)
            {
                entry.PeriodStart = now;
                entry.Count = 0;
            }

            entry.Count++;

            if (entry.Count > _options.RequestsPerMinute)
            {
                _logger.LogWarning("Rate limit exceeded for IP {IP}", ip);
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.Headers["Retry-After"] = "60";
                context.Response.ContentType = "application/json";
                context.Response.WriteAsync("{\"error\":\"Too many requests. Try again later.\"}").Wait();
                return;
            }
        }

        await _next(context);
    }
}
