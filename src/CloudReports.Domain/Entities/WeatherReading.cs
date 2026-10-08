namespace CloudReports.Domain.Entities;

/// <summary>
/// A single successful weather observation for a city, including the full raw provider payload.
/// </summary>
public sealed class WeatherReading
{
    public long Id { get; set; }

    /// <summary>City name as returned by the provider (e.g. "London").</summary>
    public required string City { get; set; }

    /// <summary>ISO 3166 country code as returned by the provider (e.g. "GB").</summary>
    public required string Country { get; set; }

    /// <summary>Current temperature in degrees Celsius.</summary>
    public double Temperature { get; set; }

    /// <summary>Minimum temperature currently observed in the area, degrees Celsius.</summary>
    public double TemperatureMin { get; set; }

    /// <summary>Maximum temperature currently observed in the area, degrees Celsius.</summary>
    public double TemperatureMax { get; set; }

    /// <summary>Time of data calculation reported by the provider (UTC).</summary>
    public DateTime ObservedAtUtc { get; set; }

    /// <summary>Time the data was fetched by this system (UTC).</summary>
    public DateTime FetchedAtUtc { get; set; }

    /// <summary>The full, unmodified JSON payload returned by the provider.</summary>
    public required string RawPayload { get; set; }
}
