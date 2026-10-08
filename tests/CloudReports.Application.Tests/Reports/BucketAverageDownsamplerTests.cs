using CloudReports.Application.Common;
using CloudReports.Application.Reports;

namespace CloudReports.Application.Tests.Reports;

public sealed class BucketAverageDownsamplerTests
{
    private static readonly DateTime Start = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Downsample_ShortRange_UsesOneMinuteBucketsAlignedToRangeStart()
    {
        var range = Range(Start, Start.AddMinutes(10));
        TemperatureSample[] samples =
        [
            new(Start.AddSeconds(2), 10, 9, 11),
            new(Start.AddMinutes(1).AddSeconds(1), 12, 11, 13),
        ];

        var result = BucketAverageDownsampler.Downsample(samples, range, maxPoints: 500);

        Assert.Equal([Start, Start.AddMinutes(1)], result.Select(s => s.TimestampUtc));
        Assert.Equal(10, result[0].Temperature);
    }

    [Fact]
    public void Downsample_DifferentSeriesInSameRange_ShareTimestamps()
    {
        var range = Range(Start, Start.AddHours(24));
        var london = CreateSamples(1_440, offsetMs: 150);
        var rome = CreateSamples(1_440, offsetMs: 420);

        var londonResult = BucketAverageDownsampler.Downsample(london, range, maxPoints: 100);
        var romeResult = BucketAverageDownsampler.Downsample(rome, range, maxPoints: 100);

        Assert.Equal(londonResult.Select(s => s.TimestampUtc), romeResult.Select(s => s.TimestampUtc));
    }

    [Fact]
    public void Downsample_LongRange_ReturnsAtMostLimitInOrder()
    {
        var range = Range(Start, Start.AddHours(24));
        var samples = CreateSamples(1_441);

        var result = BucketAverageDownsampler.Downsample(samples, range, maxPoints: 100);

        Assert.InRange(result.Count, 1, 100);
        Assert.Equal(Start, result[0].TimestampUtc);
        Assert.True(result.Zip(result.Skip(1)).All(p => p.First.TimestampUtc < p.Second.TimestampUtc));
    }

    [Fact]
    public void Downsample_PreservesExtremesAndAveragesTemperature()
    {
        var range = Range(Start, Start.AddMinutes(4));
        TemperatureSample[] samples =
        [
            new(Start, 10, 8, 12),
            new(Start.AddMinutes(1), 20, 2, 25),
            new(Start.AddMinutes(3), 30, 28, 32),
            new(Start.AddMinutes(3.5), 40, 38, 42),
        ];

        var result = BucketAverageDownsampler.Downsample(samples, range, maxPoints: 2);

        Assert.Equal(2, result.Count);
        Assert.Equal(new TemperatureSample(Start, 15, 2, 25), result[0]);
        Assert.Equal(35, result[1].Temperature);
        Assert.Equal(28, result[1].TemperatureMin);
        Assert.Equal(42, result[1].TemperatureMax);
    }

    [Fact]
    public void Downsample_InvalidLimit_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => BucketAverageDownsampler.Downsample(CreateSamples(3), Range(Start, Start.AddHours(1)), maxPoints: 0));

    private static DateRange Range(DateTime from, DateTime to) =>
        DateRange.TryCreate(new DateTimeOffset(from), new DateTimeOffset(to), to, out var range, out _)
            ? range
            : throw new InvalidOperationException();

    private static List<TemperatureSample> CreateSamples(int count, int offsetMs = 0) =>
    [
        .. Enumerable.Range(0, count)
            .Select(i => new TemperatureSample(Start.AddMinutes(i).AddMilliseconds(offsetMs), i, i - 1, i + 1)),
    ];
}
