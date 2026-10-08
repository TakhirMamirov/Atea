using CloudReports.Application.Abstractions;
using CloudReports.Application.Ingestion;
using CloudReports.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace CloudReports.Application.Tests.Ingestion;

public sealed class WeatherIngestionServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 7, 12, 0, 0, TimeSpan.Zero);

    private readonly IWeatherProvider _provider = Substitute.For<IWeatherProvider>();
    private readonly IWeatherReadingRepository _readings = Substitute.For<IWeatherReadingRepository>();
    private readonly IFetchLogRepository _logs = Substitute.For<IFetchLogRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly List<WeatherReading> _addedReadings = [];
    private readonly List<FetchLog> _addedLogs = [];

    public WeatherIngestionServiceTests()
    {
        _readings.When(r => r.Add(Arg.Any<WeatherReading>())).Do(c => _addedReadings.Add(c.Arg<WeatherReading>()));
        _logs.When(l => l.Add(Arg.Any<FetchLog>())).Do(c => _addedLogs.Add(c.Arg<FetchLog>()));
    }

    [Fact]
    public async Task IngestAsync_AllCitiesSucceed_StoresReadingAndSuccessLogPerCity()
    {
        StubSuccess("London", "GB", 12.5);
        StubSuccess("Rome", "IT", 21.0);
        var sut = CreateSut("London", "Rome");

        var summary = await sut.IngestAsync(TestContext.Current.CancellationToken);

        Assert.Equal(new IngestionSummary(2, 0), summary);
        Assert.Equal(2, _addedReadings.Count);
        Assert.Equal(2, _addedLogs.Count);
        Assert.All(_addedLogs, log =>
        {
            Assert.True(log.IsSuccess);
            Assert.Equal(200, log.HttpStatusCode);
            Assert.Null(log.ErrorMessage);
            Assert.NotNull(log.WeatherReading);
            Assert.Equal(Now.UtcDateTime, log.AttemptedAtUtc);
        });

        var london = Assert.Single(_addedReadings, r => r.City == "London");
        Assert.Equal("GB", london.Country);
        Assert.Equal(12.5, london.Temperature);
        Assert.Equal(Now.UtcDateTime, london.FetchedAtUtc);
        Assert.Equal("""{"name":"London"}""", london.RawPayload);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IngestAsync_ProviderReturnsFailure_StoresFailureLogWithoutReading()
    {
        _provider.GetCurrentWeatherAsync("Riga", Arg.Any<CancellationToken>())
            .Returns(WeatherFetchResult.Failure("Invalid API key", 401));
        var sut = CreateSut("Riga");

        var summary = await sut.IngestAsync(TestContext.Current.CancellationToken);

        Assert.Equal(new IngestionSummary(0, 1), summary);
        Assert.Empty(_addedReadings);
        var log = Assert.Single(_addedLogs);
        Assert.False(log.IsSuccess);
        Assert.Equal("Riga", log.City);
        Assert.Equal(401, log.HttpStatusCode);
        Assert.Equal("Invalid API key", log.ErrorMessage);
        Assert.Null(log.WeatherReading);
    }

    [Fact]
    public async Task IngestAsync_ProviderThrows_OtherCitiesAreStillRecorded()
    {
        StubSuccess("London", "GB", 10);
        _provider.GetCurrentWeatherAsync("Rome", Arg.Any<CancellationToken>())
            .Returns<WeatherFetchResult>(_ => throw new InvalidOperationException("boom"));
        var sut = CreateSut("London", "Rome");

        var summary = await sut.IngestAsync(TestContext.Current.CancellationToken);

        Assert.Equal(new IngestionSummary(1, 1), summary);
        var failed = Assert.Single(_addedLogs, l => !l.IsSuccess);
        Assert.Equal("Rome", failed.City);
        Assert.Contains("boom", failed.ErrorMessage, StringComparison.Ordinal);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IngestAsync_VeryLongError_IsTruncatedToColumnLength()
    {
        _provider.GetCurrentWeatherAsync("Riga", Arg.Any<CancellationToken>())
            .Returns(WeatherFetchResult.Failure(new string('x', FetchLog.MaxErrorMessageLength + 100), 500));
        var sut = CreateSut("Riga");

        await sut.IngestAsync(TestContext.Current.CancellationToken);

        Assert.Equal(FetchLog.MaxErrorMessageLength, Assert.Single(_addedLogs).ErrorMessage!.Length);
    }

    [Fact]
    public async Task IngestAsync_Cancelled_PropagatesAndDoesNotSave()
    {
        _provider.GetCurrentWeatherAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<WeatherFetchResult>(_ => throw new OperationCanceledException());
        var sut = CreateSut("London");

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => sut.IngestAsync(TestContext.Current.CancellationToken));

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private void StubSuccess(string city, string country, double temperature) =>
        _provider.GetCurrentWeatherAsync(city, Arg.Any<CancellationToken>())
            .Returns(WeatherFetchResult.Success(
                200,
                $$"""{"name":"{{city}}"}""",
                new WeatherObservation(city, country, temperature, temperature - 1, temperature + 1, Now.UtcDateTime)));

    private WeatherIngestionService CreateSut(params string[] cities) => new(
        _provider,
        _readings,
        _logs,
        _unitOfWork,
        Options.Create(new WeatherIngestionOptions { Cities = cities }),
        new FakeTimeProvider(Now),
        NullLogger<WeatherIngestionService>.Instance);
}
