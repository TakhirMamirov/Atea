using CloudReports.Application.Abstractions;
using CloudReports.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CloudReports.Infrastructure.Persistence.Repositories;

internal sealed class WeatherReadingRepository(CloudReportsDbContext dbContext) : IWeatherReadingRepository
{
    public void Add(WeatherReading reading) => dbContext.WeatherReadings.Add(reading);

    public async Task<IReadOnlyList<TemperaturePoint>> GetTemperaturePointsAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken) =>
        await dbContext.WeatherReadings
            .AsNoTracking()
            .Where(r => r.FetchedAtUtc >= fromUtc && r.FetchedAtUtc <= toUtc)
            .OrderBy(r => r.FetchedAtUtc)
            .Select(r => new TemperaturePoint(
                r.City,
                r.Country,
                r.FetchedAtUtc,
                r.ObservedAtUtc,
                r.Temperature,
                r.TemperatureMin,
                r.TemperatureMax))
            .ToListAsync(cancellationToken);
}
