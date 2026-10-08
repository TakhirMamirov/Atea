using CloudReports.Application.Abstractions;
using CloudReports.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CloudReports.Application.Ingestion;

internal sealed partial class WeatherIngestionService(
    IWeatherProvider weatherProvider,
    IWeatherReadingRepository readings,
    IFetchLogRepository fetchLogs,
    IUnitOfWork unitOfWork,
    IOptions<WeatherIngestionOptions> options,
    TimeProvider timeProvider,
    ILogger<WeatherIngestionService> logger) : IWeatherIngestionService
{
    public async Task<IngestionSummary> IngestAsync(CancellationToken cancellationToken)
    {
        // Provider calls are independent and I/O bound, so they run concurrently.
        // Persistence happens afterwards on a single thread because the unit of work is not thread-safe.
        var attempts = await Task.WhenAll(options.Value.Cities.Select(city => FetchAsync(city, cancellationToken)));

        foreach (var attempt in attempts)
        {
            Record(attempt);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var succeeded = attempts.Count(a => a.Result.IsSuccess);
        var summary = new IngestionSummary(succeeded, attempts.Length - succeeded);
        LogIngestionFinished(summary.Succeeded, summary.Failed);

        return summary;
    }

    private async Task<FetchAttempt> FetchAsync(string city, CancellationToken cancellationToken)
    {
        var attemptedAt = timeProvider.GetUtcNow().UtcDateTime;
        var started = timeProvider.GetTimestamp();

        WeatherFetchResult result;
        try
        {
            result = await weatherProvider.GetCurrentWeatherAsync(city, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Defensive: a misbehaving provider must not prevent other cities from being recorded.
            result = WeatherFetchResult.Failure($"Unexpected error: {ex.Message}");
        }

        var duration = timeProvider.GetElapsedTime(started);
        return new FetchAttempt(city, attemptedAt, duration, result);
    }

    private void Record(FetchAttempt attempt)
    {
        var (city, attemptedAt, duration, result) = attempt;

        var log = new FetchLog
        {
            City = city,
            AttemptedAtUtc = attemptedAt,
            IsSuccess = result.IsSuccess,
            HttpStatusCode = result.HttpStatusCode,
            DurationMs = (long)duration.TotalMilliseconds,
            ErrorMessage = Truncate(result.Error, FetchLog.MaxErrorMessageLength),
        };

        if (result is { IsSuccess: true, Observation: { } observation, RawPayload: { } payload })
        {
            var reading = new WeatherReading
            {
                City = observation.City,
                Country = observation.Country,
                Temperature = observation.Temperature,
                TemperatureMin = observation.TemperatureMin,
                TemperatureMax = observation.TemperatureMax,
                ObservedAtUtc = observation.ObservedAtUtc,
                FetchedAtUtc = attemptedAt,
                RawPayload = payload,
            };

            readings.Add(reading);
            log.WeatherReading = reading;
        }
        else
        {
            LogFetchFailed(city, result.Error);
        }

        fetchLogs.Add(log);
    }

    private static string? Truncate(string? value, int maxLength) =>
        value is { Length: > 0 } && value.Length > maxLength ? value[..maxLength] : value;

    [LoggerMessage(Level = LogLevel.Information, Message = "Weather ingestion finished: {Succeeded} succeeded, {Failed} failed")]
    private partial void LogIngestionFinished(int succeeded, int failed);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Fetching weather for {City} failed: {Error}")]
    private partial void LogFetchFailed(string city, string? error);

    private sealed record FetchAttempt(string City, DateTime AttemptedAtUtc, TimeSpan Duration, WeatherFetchResult Result);
}
