using System.ComponentModel.DataAnnotations;

namespace CloudReports.Application.Ingestion;

public sealed class WeatherIngestionOptions
{
    public const string SectionName = "WeatherIngestion";

    /// <summary>City names passed to the provider as the <c>q</c> parameter.</summary>
    [Required]
    [MinLength(1)]
    public IList<string> Cities { get; init; } = [];
}
