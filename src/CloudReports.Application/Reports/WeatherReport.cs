namespace CloudReports.Application.Reports;

public sealed record WeatherReport(
    DateTime FromUtc,
    DateTime ToUtc,
    IReadOnlyList<CitySeries> Series,
    IReadOnlyList<CitySummary> Cities);

/// <summary>Temperature time series for one city.</summary>
public sealed record CitySeries(string City, string Country, IReadOnlyList<TemperatureSample> Points);

/// <summary>One point of a temperature series. When downsampled, values are aggregated over the bucket.</summary>
public sealed record TemperatureSample(
    DateTime TimestampUtc,
    double Temperature,
    double TemperatureMin,
    double TemperatureMax);

/// <summary>Aggregated figures for one city within the selected range.</summary>
public sealed record CitySummary(
    string City,
    string Country,
    double CurrentTemperature,
    double MinTemperature,
    double MaxTemperature,
    DateTime LastObservedAtUtc,
    DateTime LastFetchedAtUtc,
    int SampleCount);
