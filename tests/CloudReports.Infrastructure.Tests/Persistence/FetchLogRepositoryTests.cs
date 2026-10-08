using CloudReports.Application.Logs;
using CloudReports.Domain.Entities;
using CloudReports.Infrastructure.Persistence.Repositories;

namespace CloudReports.Infrastructure.Tests.Persistence;

public sealed class FetchLogRepositoryTests
{
    private static readonly DateTime Now = new(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetPageAsync_ReturnsNewestFirstWithTotalCount()
    {
        await using var db = await SeedAsync();
        await using var context = db.CreateContext();

        var page = await new FetchLogRepository(context)
            .GetPageAsync(new FetchLogQuery(Page: 2, PageSize: 2), TestContext.Current.CancellationToken);

        Assert.Equal(5, page.TotalCount);
        Assert.Equal(3, page.TotalPages);
        Assert.Equal([Now.AddMinutes(-3), Now.AddMinutes(-4)], page.Items.Select(l => l.AttemptedAtUtc));
    }

    [Fact]
    public async Task GetPageAsync_FiltersByCityAndStatus()
    {
        await using var db = await SeedAsync();
        await using var context = db.CreateContext();

        var page = await new FetchLogRepository(context)
            .GetPageAsync(new FetchLogQuery(City: "Riga", IsSuccess: false), TestContext.Current.CancellationToken);

        var log = Assert.Single(page.Items);
        Assert.Equal("Riga", log.City);
        Assert.False(log.IsSuccess);
    }

    [Fact]
    public async Task Add_SuccessLogWithReading_PersistsRelationship()
    {
        await using var db = await SqliteDbContextFixture.CreateAsync();
        await using (var context = db.CreateContext())
        {
            var reading = new WeatherReading { City = "Rome", Country = "IT", RawPayload = "{}", FetchedAtUtc = Now, ObservedAtUtc = Now };
            context.WeatherReadings.Add(reading);
            new FetchLogRepository(context).Add(new FetchLog { City = "Rome", AttemptedAtUtc = Now, IsSuccess = true, WeatherReading = reading });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var readContext = db.CreateContext();
        var stored = Assert.Single(readContext.FetchLogs);
        Assert.NotNull(stored.WeatherReadingId);
    }

    private static async Task<SqliteDbContextFixture> SeedAsync()
    {
        var db = await SqliteDbContextFixture.CreateAsync();
        await using var context = db.CreateContext();
        var repository = new FetchLogRepository(context);
        repository.Add(Log("London", minutesAgo: 1, success: true));
        repository.Add(Log("Rome", minutesAgo: 2, success: true));
        repository.Add(Log("Riga", minutesAgo: 3, success: false));
        repository.Add(Log("Riga", minutesAgo: 4, success: true));
        repository.Add(Log("London", minutesAgo: 5, success: true));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return db;
    }

    private static FetchLog Log(string city, int minutesAgo, bool success) => new()
    {
        City = city,
        AttemptedAtUtc = Now.AddMinutes(-minutesAgo),
        IsSuccess = success,
        HttpStatusCode = success ? 200 : 500,
        DurationMs = 100,
    };
}
