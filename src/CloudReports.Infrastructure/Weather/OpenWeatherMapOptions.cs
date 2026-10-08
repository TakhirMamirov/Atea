using System.ComponentModel.DataAnnotations;

namespace CloudReports.Infrastructure.Weather;

public sealed class OpenWeatherMapOptions
{
    public const string SectionName = "OpenWeatherMap";

    [Required]
    public Uri BaseUrl { get; init; } = new("https://api.openweathermap.org/");

    [Required(AllowEmptyStrings = false)]
    public string ApiKey { get; init; } = string.Empty;

    /// <summary>OpenWeatherMap unit system. "metric" returns degrees Celsius.</summary>
    [Required]
    public string Units { get; init; } = "metric";

    [Range(1, 60)]
    public int TimeoutSeconds { get; init; } = 10;
}
