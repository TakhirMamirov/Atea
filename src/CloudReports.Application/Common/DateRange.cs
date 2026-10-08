using System.Diagnostics.CodeAnalysis;

namespace CloudReports.Application.Common;

/// <summary>A validated, closed UTC time interval.</summary>
public sealed record DateRange
{
    public static readonly TimeSpan DefaultSpan = TimeSpan.FromHours(24);
    public static readonly TimeSpan MaxSpan = TimeSpan.FromDays(31);

    private DateRange(DateTime fromUtc, DateTime toUtc)
    {
        FromUtc = fromUtc;
        ToUtc = toUtc;
    }

    public DateTime FromUtc { get; }

    public DateTime ToUtc { get; }

    /// <summary>
    /// Builds a range from optional bounds. Missing bounds default to the last <see cref="DefaultSpan"/> ending now.
    /// </summary>
    /// <returns><c>true</c> with the range, or <c>false</c> with an error message when the bounds are invalid.</returns>
    public static bool TryCreate(
        DateTimeOffset? from,
        DateTimeOffset? to,
        DateTime utcNow,
        [NotNullWhen(true)] out DateRange? range,
        [NotNullWhen(false)] out string? error)
    {
        var toUtc = to?.UtcDateTime ?? utcNow;
        var fromUtc = from?.UtcDateTime ?? toUtc - DefaultSpan;

        range = null;

        if (fromUtc >= toUtc)
        {
            error = "'from' must be earlier than 'to'.";
            return false;
        }

        if (toUtc - fromUtc > MaxSpan)
        {
            error = $"The selected range must not exceed {MaxSpan.TotalDays:0} days.";
            return false;
        }

        error = null;
        range = new DateRange(fromUtc, toUtc);
        return true;
    }
}
