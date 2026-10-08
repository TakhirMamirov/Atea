using System.Net;
using System.Net.Http.Json;
using CloudReports.Application.Common;
using CloudReports.Application.Logs;
using CloudReports.Application.Reports;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace CloudReports.Web.Tests.Api;

/// <summary>
/// Runs the real ASP.NET Core pipeline in memory (routing, model binding, validation, error responses).
/// Only the application services are replaced with fakes, so no database is needed.
/// </summary>
public sealed class ApiTests : IClassFixture<ApiTests.ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public ApiTests(ApiFactory factory)
    {
        _factory = factory;
        _factory.ReportService.ClearReceivedCalls();
        _factory.LogQueryService.ClearReceivedCalls();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetWeather_NoParameters_ReturnsReport()
    {
        var response = await _client.GetAsync(new Uri("/api/weather", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        await _factory.ReportService.Received(1).GetReportAsync(Arg.Any<DateRange>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("/api/weather?from=not-a-date")]
    [InlineData("/api/weather?from=2026-10-07T10:00:00Z&to=2026-10-06T10:00:00Z")]
    [InlineData("/api/weather?from=2026-08-01T00:00:00Z&to=2026-10-01T00:00:00Z")]
    [InlineData("/api/fetch-logs?page=abc")]
    [InlineData("/api/fetch-logs?isSuccess=maybe")]
    public async Task InvalidParameters_ReturnValidationProblem(string url)
    {
        var response = await _client.GetAsync(new Uri(url, UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestContext.Current.CancellationToken);
        Assert.NotEmpty(problem!.Errors);
    }

    [Fact]
    public async Task GetFetchLogs_BindsFiltersFromQueryString()
    {
        var response = await _client.GetAsync(
            new Uri("/api/fetch-logs?city=Riga&isSuccess=false&page=2&pageSize=10", UriKind.Relative),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await _factory.LogQueryService.Received(1).GetLogsAsync(
            new FetchLogQuery(2, 10, "Riga", false),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UnknownApiRoute_ReturnsNotFound()
    {
        var response = await _client.GetAsync(new Uri("/api/does-not-exist", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    public sealed class ApiFactory : WebApplicationFactory<Program>
    {
        public IWeatherReportService ReportService { get; } = Substitute.For<IWeatherReportService>();

        public IFetchLogQueryService LogQueryService { get; } = Substitute.For<IFetchLogQueryService>();

        public ApiFactory()
        {
            ReportService.GetReportAsync(Arg.Any<DateRange>(), Arg.Any<CancellationToken>())
                .Returns(c => new WeatherReport(c.Arg<DateRange>().FromUtc, c.Arg<DateRange>().ToUtc, [], []));
            LogQueryService.GetLogsAsync(Arg.Any<FetchLogQuery>(), Arg.Any<CancellationToken>())
                .Returns(c => new PagedResult<FetchLogDto>([], 0, c.Arg<FetchLogQuery>().Page, c.Arg<FetchLogQuery>().PageSize));
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:CloudReports", "Server=unused;Database=unused");
            builder.UseSetting("Database:MigrateOnStartup", "false");
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton(ReportService);
                services.AddSingleton(LogQueryService);
            });
        }
    }
}
