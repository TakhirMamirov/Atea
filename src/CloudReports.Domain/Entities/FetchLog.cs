namespace CloudReports.Domain.Entities;

/// <summary>
/// Audit record of a single attempt to fetch weather data for a city.
/// </summary>
public sealed class FetchLog
{
    public const int MaxErrorMessageLength = 2000;

    public long Id { get; set; }

    /// <summary>City name that was requested.</summary>
    public required string City { get; set; }

    public DateTime AttemptedAtUtc { get; set; }

    public bool IsSuccess { get; set; }

    /// <summary>HTTP status code returned by the provider, or null when no response was received.</summary>
    public int? HttpStatusCode { get; set; }

    public long DurationMs { get; set; }

    public string? ErrorMessage { get; set; }

    public long? WeatherReadingId { get; set; }

    public WeatherReading? WeatherReading { get; set; }
}
