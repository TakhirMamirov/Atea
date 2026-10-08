using System.Net;
using Microsoft.AspNetCore.Hosting;

namespace CloudReports.Web.Tests.Api;

public sealed class RateLimitingTests : IClassFixture<RateLimitingTests.LowLimitApiFactory>
{
    private readonly HttpClient _client;

    public RateLimitingTests(LowLimitApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task RequestsOverTheLimit_AreRejectedWith429()
    {
        var uri = new Uri("/api/fetch-logs", UriKind.Relative);

        var first = await _client.GetAsync(uri, TestContext.Current.CancellationToken);
        var second = await _client.GetAsync(uri, TestContext.Current.CancellationToken);
        var third = await _client.GetAsync(uri, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
        Assert.Equal("application/problem+json", third.Content.Headers.ContentType?.MediaType);
    }

    /// <summary>Same app, but only two API requests per minute are allowed.</summary>
    public sealed class LowLimitApiFactory : ApiTests.ApiFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseSetting("RateLimiting:PermitLimit", "2");
        }
    }
}
