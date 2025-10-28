namespace Novibet.Application.Common.Interfaces;

public interface ICacheableRequest
{
    string CacheKey { get; }
    TimeSpan? SlidingExpiration { get; }
}