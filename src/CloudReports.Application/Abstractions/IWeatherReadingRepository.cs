using CloudReports.Domain.Entities;

namespace CloudReports.Application.Abstractions;

/// <summary>Stores weather readings and queries them for the chart.</summary>
public interface IWeatherReadingRepository
{
    /// <summary>Adds a reading; it is saved on the next <see cref="IUnitOfWork.SaveChangesAsync"/>.</summary>
    void Add(WeatherReading reading);

    /// <summary>Returns lightweight temperature points fetched within [fromUtc, toUtc], ordered by fetch time.</summary>
    Task<IReadOnlyList<TemperaturePoint>> GetTemperaturePointsAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken);
}

/// <summary>Read model projected from <see cref="WeatherReading"/> without the raw payload.</summary>
public sealed record TemperaturePoint(
    string City,
    string Country,
    DateTime FetchedAtUtc,
    DateTime ObservedAtUtc,
    double Temperature,
    double TemperatureMin,
    double TemperatureMax);
