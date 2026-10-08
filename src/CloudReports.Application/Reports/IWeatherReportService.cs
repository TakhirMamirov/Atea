using CloudReports.Application.Common;

namespace CloudReports.Application.Reports;

/// <summary>Builds the data for the weather page: chart series and per-city summaries.</summary>
public interface IWeatherReportService
{
    /// <summary>Returns a temperature series and a summary for each city with data in the range.</summary>
    Task<WeatherReport> GetReportAsync(DateRange range, CancellationToken cancellationToken);
}
