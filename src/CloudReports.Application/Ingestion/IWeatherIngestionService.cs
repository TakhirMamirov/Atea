namespace CloudReports.Application.Ingestion;

/// <summary>Fetches the current weather for all configured cities and persists payloads and attempt logs.</summary>
public interface IWeatherIngestionService
{
    /// <summary>Runs one ingestion cycle; a failure for one city does not prevent the others from being saved.</summary>
    Task<IngestionSummary> IngestAsync(CancellationToken cancellationToken);
}

/// <summary>Number of cities fetched successfully and unsuccessfully in one cycle.</summary>
public sealed record IngestionSummary(int Succeeded, int Failed);
