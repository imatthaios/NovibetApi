using System.Net;
using Microsoft.Extensions.Logging;

namespace Novibet.Infrastructure.Helpers;

public class HttpClientHelper : IHttpClientHelper, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpClientHelper> _logger;

    public HttpClientHelper(ILogger<HttpClientHelper> logger)
    {
        _logger = logger;
        
        _httpClient = new HttpClient(new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        });
        
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Novibet/1.0");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<string> GetStringAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Making HTTP GET request to {Url}", url);
            
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogDebug("Successfully received response from {Url}, Content length: {Length}", 
                url, content.Length);
            
            return content;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed for {Url}", url);
            throw;
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "HTTP request timeout for {Url}", url);
            throw new TimeoutException($"Request to {url} timed out", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during HTTP request to {Url}", url);
            throw;
        }
    }

    public async Task<HttpResponseMessage> GetAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Making HTTP GET request to {Url}", url);
            var response = await _httpClient.GetAsync(url, cancellationToken);
            _logger.LogDebug("HTTP request to {Url} completed with status {StatusCode}", 
                url, response.StatusCode);
            
            return response;
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "HTTP request timeout for {Url}", url);
            throw new TimeoutException($"Request to {url} timed out", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during HTTP request to {Url}", url);
            throw;
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}