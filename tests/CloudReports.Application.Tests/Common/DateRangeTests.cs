using CloudReports.Application.Common;

namespace CloudReports.Application.Tests.Common;

public sealed class DateRangeTests
{
    private static readonly DateTime Now = new(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void TryCreate_NoBounds_DefaultsToLast24Hours()
    {
        var created = DateRange.TryCreate(null, null, Now, out var range, out var error);

        Assert.True(created);
        Assert.Null(error);
        Assert.Equal(Now, range!.ToUtc);
        Assert.Equal(Now - DateRange.DefaultSpan, range.FromUtc);
    }

    [Fact]
    public void TryCreate_OffsetBounds_AreConvertedToUtc()
    {
        var from = new DateTimeOffset(2026, 10, 7, 10, 0, 0, TimeSpan.FromHours(3));
        var to = new DateTimeOffset(2026, 10, 7, 12, 0, 0, TimeSpan.FromHours(3));

        Assert.True(DateRange.TryCreate(from, to, Now, out var range, out _));
        Assert.Equal(new DateTime(2026, 10, 7, 7, 0, 0, DateTimeKind.Utc), range.FromUtc);
        Assert.Equal(DateTimeKind.Utc, range.FromUtc.Kind);
    }

    [Fact]
    public void TryCreate_FromAfterTo_ReturnsError()
    {
        var created = DateRange.TryCreate(
            new DateTimeOffset(Now), new DateTimeOffset(Now.AddHours(-1)), Now, out var range, out var error);

        Assert.False(created);
        Assert.Null(range);
        Assert.Equal("'from' must be earlier than 'to'.", error);
    }

    [Fact]
    public void TryCreate_RangeTooLong_ReturnsError()
    {
        var created = DateRange.TryCreate(
            new DateTimeOffset(Now - DateRange.MaxSpan - TimeSpan.FromMinutes(1)),
            new DateTimeOffset(Now),
            Now,
            out var range,
            out var error);

        Assert.False(created);
        Assert.Null(range);
        Assert.Contains("31", error, StringComparison.Ordinal);
    }
}
