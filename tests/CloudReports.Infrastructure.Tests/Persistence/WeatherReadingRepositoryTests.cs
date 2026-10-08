using CloudReports.Domain.Entities;
using CloudReports.Infrastructure.Persistence.Repositories;

namespace CloudReports.Infrastructure.Tests.Persistence;

public sealed class WeatherReadingRepositoryTests
{
    private static readonly DateTime Now = new(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetTemperaturePointsAsync_ReturnsOnlyPointsInRangeOrderedByFetchTime()
    {
        await using var db = await SqliteDbContextFixture.CreateAsync();
        await using (var context = db.CreateContext())
        {
            var repository = new WeatherReadingRepository(context);
            repository.Add(Reading("London", Now.AddHours(-30)));
            repository.Add(Reading("Rome", Now.AddMinutes(-1)));
            repository.Add(Reading("London", Now.AddMinutes(-2)));
            repository.Add(Reading("Riga", Now.AddMinutes(5)));
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var readContext = db.CreateContext();
        var points = await new WeatherReadingRepository(readContext)
            .GetTemperaturePointsAsync(Now.AddHours(-24), Now, TestContext.Current.CancellationToken);

        Assert.Equal(["London", "Rome"], points.Select(p => p.City));
        Assert.All(points, p => Assert.Equal(DateTimeKind.Utc, p.FetchedAtUtc.Kind));
        Assert.Equal(Now.AddMinutes(-2), points[0].FetchedAtUtc);
    }

    private static WeatherReading Reading(string city, DateTime fetchedAt) => new()
    {
        City = city,
        Country = "XX",
        Temperature = 10,
        TemperatureMin = 9,
        TemperatureMax = 11,
        ObservedAtUtc = fetchedAt.AddMinutes(-5),
        FetchedAtUtc = fetchedAt,
        RawPayload = "{}",
    };
}
