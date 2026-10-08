using CloudReports.Application.Abstractions;
using CloudReports.Application.Common;
using CloudReports.Application.Logs;
using CloudReports.Domain.Entities;

namespace CloudReports.Application.Tests.Logs;

public sealed class FetchLogQueryServiceTests
{
    private readonly IFetchLogRepository _repository = Substitute.For<IFetchLogRepository>();

    [Fact]
    public async Task GetLogsAsync_NormalizesQueryAndMapsEntities()
    {
        var attemptedAt = new DateTime(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc);
        FetchLog[] logs =
        [
            new() { Id = 7, City = "Rome", AttemptedAtUtc = attemptedAt, IsSuccess = false, HttpStatusCode = 404, DurationMs = 15, ErrorMessage = "city not found" },
        ];
        _repository.GetPageAsync(Arg.Any<FetchLogQuery>(), Arg.Any<CancellationToken>())
            .Returns(c => new PagedResult<FetchLog>(logs, 1, c.Arg<FetchLogQuery>().Page, c.Arg<FetchLogQuery>().PageSize));
        var sut = new FetchLogQueryService(_repository);

        var result = await sut.GetLogsAsync(new FetchLogQuery(Page: 0, PageSize: 10_000, City: "  "), TestContext.Current.CancellationToken);

        await _repository.Received(1).GetPageAsync(
            new FetchLogQuery(1, FetchLogQuery.MaxPageSize, null, null),
            Arg.Any<CancellationToken>());
        Assert.Equal(new FetchLogDto(7, "Rome", attemptedAt, false, 404, 15, "city not found"), Assert.Single(result.Items));
        Assert.Equal(1, result.TotalPages);
    }
}
