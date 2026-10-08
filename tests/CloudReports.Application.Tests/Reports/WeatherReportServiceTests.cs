using CloudReports.Application.Abstractions;
using CloudReports.Application.Common;
using CloudReports.Application.Reports;

namespace CloudReports.Application.Tests.Reports;

public sealed class WeatherReportServiceTests
{
    private static readonly DateTime Now = new(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc);

    private readonly IWeatherReadingRepository _readings = Substitute.For<IWeatherReadingRepository>();
    private readonly WeatherReportService _sut;

    public WeatherReportServiceTests() => _sut = new WeatherReportService(_readings);

    [Fact]
    public async Task GetReportAsync_GroupsByCityAndComputesSummary()
    {
        StubPoints(
            Point("Rome", "IT", minutesAgo: 2, temp: 20, min: 18, max: 22),
            Point("London", "GB", minutesAgo: 2, temp: 10, min: 9, max: 11),
            Point("London", "GB", minutesAgo: 1, temp: 12, min: 7, max: 14),
            Point("Rome", "IT", minutesAgo: 1, temp: 21, min: 19, max: 25));

        var report = await _sut.GetReportAsync(Last24Hours(), TestContext.Current.CancellationToken);

        Assert.Equal(["London", "Rome"], report.Series.Select(s => s.City));
        Assert.All(report.Series, s => Assert.NotEmpty(s.Points));

        var london = Assert.Single(report.Cities, c => c.City == "London");
        Assert.Equal("GB", london.Country);
        Assert.Equal(12, london.CurrentTemperature);
        Assert.Equal(7, london.MinTemperature);
        Assert.Equal(14, london.MaxTemperature);
        Assert.Equal(Now.AddMinutes(-1), london.LastFetchedAtUtc);
        Assert.Equal(2, london.SampleCount);
    }

    [Fact]
    public async Task GetReportAsync_NoData_ReturnsEmptyReportWithRange()
    {
        StubPoints();
        var range = Last24Hours();

        var report = await _sut.GetReportAsync(range, TestContext.Current.CancellationToken);

        Assert.Empty(report.Series);
        Assert.Empty(report.Cities);
        Assert.Equal(range.FromUtc, report.FromUtc);
        Assert.Equal(range.ToUtc, report.ToUtc);
    }

    [Fact]
    public async Task GetReportAsync_ManyPoints_SeriesIsDownsampled()
    {
        StubPoints([.. Enumerable.Range(1, 2_000).Select(i => Point("Riga", "LV", minutesAgo: 2_001 - i, temp: i, min: i, max: i))]);

        var report = await _sut.GetReportAsync(Last24Hours(), TestContext.Current.CancellationToken);

        Assert.InRange(Assert.Single(report.Series).Points.Count, 1, WeatherReportService.MaxPointsPerCity);
        Assert.Equal(2_000, Assert.Single(report.Cities).SampleCount);
    }

    private void StubPoints(params TemperaturePoint[] points) =>
        _readings.GetTemperaturePointsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(points);

    private static DateRange Last24Hours() =>
        DateRange.TryCreate(null, null, Now, out var range, out _) ? range : throw new InvalidOperationException();

    private static TemperaturePoint Point(string city, string country, int minutesAgo, double temp, double min, double max) =>
        new(city, country, Now.AddMinutes(-minutesAgo), Now.AddMinutes(-minutesAgo - 5), temp, min, max);
}
