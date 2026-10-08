namespace CloudReports.Application.Abstractions;

/// <summary>
/// Retrieves current weather data from an external provider.
/// Implementations must never throw for expected failures; they report them through <see cref="WeatherFetchResult"/>.
/// </summary>
public interface IWeatherProvider
{
    /// <summary>Fetches the current weather for a city; returns a failure result instead of throwing.</summary>
    Task<WeatherFetchResult> GetCurrentWeatherAsync(string city, CancellationToken cancellationToken);
}

/// <summary>Normalized weather values extracted from a provider payload.</summary>
public sealed record WeatherObservation(
    string City,
    string Country,
    double Temperature,
    double TemperatureMin,
    double TemperatureMax,
    DateTime ObservedAtUtc);

/// <summary>Outcome of a single provider call.</summary>
public sealed record WeatherFetchResult
{
    private WeatherFetchResult() { }

    public bool IsSuccess { get; private init; }

    public int? HttpStatusCode { get; private init; }

    public string? RawPayload { get; private init; }

    public WeatherObservation? Observation { get; private init; }

    public string? Error { get; private init; }

    public static WeatherFetchResult Success(int httpStatusCode, string rawPayload, WeatherObservation observation) => new()
    {
        IsSuccess = true,
        HttpStatusCode = httpStatusCode,
        RawPayload = rawPayload,
        Observation = observation,
    };

    public static WeatherFetchResult Failure(string error, int? httpStatusCode = null) => new()
    {
        IsSuccess = false,
        HttpStatusCode = httpStatusCode,
        Error = error,
    };
}
