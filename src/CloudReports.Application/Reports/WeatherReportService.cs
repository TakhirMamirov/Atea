using CloudReports.Application.Abstractions;
using CloudReports.Application.Common;

namespace CloudReports.Application.Reports;

internal sealed class WeatherReportService(IWeatherReadingRepository readings) : IWeatherReportService
{
    /// <summary>Upper bound of points per city returned to the chart.</summary>
    internal const int MaxPointsPerCity = 500;

    public async Task<WeatherReport> GetReportAsync(DateRange range, CancellationToken cancellationToken)
    {
        var points = await readings.GetTemperaturePointsAsync(range.FromUtc, range.ToUtc, cancellationToken);

        // Points arrive ordered by fetch time, and GroupBy keeps that order within each city.
        var byCity = points
            .GroupBy(p => p.City, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.ToList())
            .ToList();

        var series = byCity.Select(cityPoints => BuildSeries(cityPoints, range)).ToList();
        var summaries = byCity.Select(BuildSummary).ToList();

        return new WeatherReport(range.FromUtc, range.ToUtc, series, summaries);
    }

    private static CitySeries BuildSeries(List<TemperaturePoint> cityPoints, DateRange range)
    {
        var latest = cityPoints[^1];
        var samples = cityPoints
            .Select(p => new TemperatureSample(p.FetchedAtUtc, p.Temperature, p.TemperatureMin, p.TemperatureMax))
            .ToList();

        return new CitySeries(latest.City, latest.Country, BucketAverageDownsampler.Downsample(samples, range, MaxPointsPerCity));
    }

    private static CitySummary BuildSummary(List<TemperaturePoint> cityPoints)
    {
        var latest = cityPoints[^1];
        return new CitySummary(
            latest.City,
            latest.Country,
            latest.Temperature,
            cityPoints.Min(p => p.TemperatureMin),
            cityPoints.Max(p => p.TemperatureMax),
            latest.ObservedAtUtc,
            latest.FetchedAtUtc,
            cityPoints.Count);
    }
}
