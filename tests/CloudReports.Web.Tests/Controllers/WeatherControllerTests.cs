using CloudReports.Application.Common;
using CloudReports.Application.Reports;
using CloudReports.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Time.Testing;

namespace CloudReports.Web.Tests.Controllers;

public sealed class WeatherControllerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 7, 12, 0, 0, TimeSpan.Zero);

    private readonly IWeatherReportService _reportService = Substitute.For<IWeatherReportService>();
    private readonly WeatherController _sut;

    public WeatherControllerTests() => _sut = new WeatherController(_reportService, new FakeTimeProvider(Now));

    [Fact]
    public async Task GetReport_NoBounds_RequestsLast24Hours()
    {
        var expected = new WeatherReport(Now.UtcDateTime.AddDays(-1), Now.UtcDateTime, [], []);
        _reportService.GetReportAsync(Arg.Any<DateRange>(), Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.GetReport(null, null, TestContext.Current.CancellationToken);

        Assert.Same(expected, result.Value);
        await _reportService.Received(1).GetReportAsync(
            Arg.Is<DateRange>(r => r.ToUtc == Now.UtcDateTime && r.FromUtc == Now.UtcDateTime.AddDays(-1)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetReport_InvalidRange_ReturnsValidationProblem()
    {
        var result = await _sut.GetReport(Now, Now.AddHours(-1), TestContext.Current.CancellationToken);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.IsType<ValidationProblemDetails>(objectResult.Value);
        await _reportService.DidNotReceive().GetReportAsync(Arg.Any<DateRange>(), Arg.Any<CancellationToken>());
    }
}
