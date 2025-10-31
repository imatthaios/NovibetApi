using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Novibet.Application.Common.Interfaces;
using Novibet.Infrastructure.Helpers;
using Novibet.Infrastructure.Options;
using Novibet.Infrastructure.Services.EcbGateway;

namespace Novibet.Tests.Services;

public class EcbRateServiceTests
{
    private readonly Mock<IApplicationDbContext> _db = new();
    private readonly Mock<IHttpClientHelper> _http = new();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly ILogger<EcbRateService> _logger = Mock.Of<ILogger<EcbRateService>>();
    private readonly EcbOptions _options = new() { Url = "https://mock.ecb.xml" };

    [Fact]
    public async Task UpdateRatesAsync_ShouldParseAndCacheRates()
    {
        // Arrange
        var xml = "<Envelope><Cube><Cube time='2025-10-31'><Cube currency='USD' rate='1.1'/></Cube></Cube></Envelope>";
        _http.Setup(h => h.GetStringAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(xml);

        var service = new EcbRateService(_cache, _db.Object, Options.Create(_options), _logger, _http.Object);

        // Act
        await service.UpdateRatesAsync(CancellationToken.None);

        // Assert
        _cache.TryGetValue("ecb_rates_20251031", out _).Should().BeTrue();
    }
}