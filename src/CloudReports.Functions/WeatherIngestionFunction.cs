using CloudReports.Application.Ingestion;
using Microsoft.Azure.Functions.Worker;

namespace CloudReports.Functions;

/// <summary>Fetches and stores the current weather for all configured cities once per minute.</summary>
public sealed class WeatherIngestionFunction(IWeatherIngestionService ingestionService)
{
    /// <summary>NCRONTAB expression resolved from the <c>WeatherIngestionSchedule</c> app setting ("0 * * * * *").</summary>
    public const string Schedule = "%WeatherIngestionSchedule%";

    [Function(nameof(WeatherIngestionFunction))]
    public async Task Run(
        [TimerTrigger(Schedule)] TimerInfo timer,
        CancellationToken cancellationToken) =>
        await ingestionService.IngestAsync(cancellationToken);
}
