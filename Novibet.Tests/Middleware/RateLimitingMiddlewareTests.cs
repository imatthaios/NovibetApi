using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Novibet.Api.Middleware;
using Novibet.Application.Options;

namespace Novibet.Tests.Middleware;

public class RateLimitingMiddlewareTests
{
    private readonly Mock<ILogger<RateLimitingMiddleware>> _loggerMock;
    private readonly RequestDelegate _nextDelegate;

    public RateLimitingMiddlewareTests()
    {
        _loggerMock = new Mock<ILogger<RateLimitingMiddleware>>();
        _nextDelegate = (context) =>
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            return Task.CompletedTask;
        };
    }

    [Fact]
    public async Task Allows_Request_Under_Limit()
    {
        // Arrange
        var options = Options.Create(new RateLimitingOptions { RequestsPerMinute = 5 });
        var middleware = new RateLimitingMiddleware(_nextDelegate, options, _loggerMock.Object);

        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task Blocks_Request_Over_Limit()
    {
        var options = Options.Create(new RateLimitingOptions { RequestsPerMinute = 2 });
        var middleware = new RateLimitingMiddleware(_nextDelegate, options, _loggerMock.Object);

        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("192.168.1.5");

        // 3 sequential requests
        await middleware.InvokeAsync(context);
        await middleware.InvokeAsync(context);
        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status429TooManyRequests, context.Response.StatusCode);
    }

    [Fact]
    public async Task Resets_After_One_Minute()
    {
        var options = Options.Create(new RateLimitingOptions { RequestsPerMinute = 1 });
        var middleware = new RateLimitingMiddleware(_nextDelegate, options, _loggerMock.Object);

        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.7");

        await middleware.InvokeAsync(context);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);

        await Task.Delay(TimeSpan.FromSeconds(61));

        await middleware.InvokeAsync(context);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task Tracks_Separate_IPs_Independently()
    {
        var options = Options.Create(new RateLimitingOptions { RequestsPerMinute = 1 });
        var middleware = new RateLimitingMiddleware(_nextDelegate, options, _loggerMock.Object);

        var context1 = new DefaultHttpContext();
        var context2 = new DefaultHttpContext();

        context1.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.1");
        context2.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.2");

        await middleware.InvokeAsync(context1);
        await middleware.InvokeAsync(context2);

        Assert.Equal(StatusCodes.Status200OK, context1.Response.StatusCode);
        Assert.Equal(StatusCodes.Status200OK, context2.Response.StatusCode);
    }
}
