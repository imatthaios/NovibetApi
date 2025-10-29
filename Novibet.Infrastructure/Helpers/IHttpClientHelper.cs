namespace Novibet.Infrastructure.Helpers;

public interface IHttpClientHelper
{
    Task<string> GetStringAsync(string url, CancellationToken cancellationToken = default);
    Task<HttpResponseMessage> GetAsync(string url, CancellationToken cancellationToken = default);
}