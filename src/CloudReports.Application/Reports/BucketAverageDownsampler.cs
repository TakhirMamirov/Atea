using CloudReports.Application.Common;

namespace CloudReports.Application.Reports;

/// <summary>
/// Reduces a temperature series to a bounded number of points for the chart.
/// The range is split into equal-width buckets starting at <see cref="DateRange.FromUtc"/>, so every city's series
/// shares the same timestamps. Each non-empty bucket yields one sample stamped with the bucket start, holding the
/// average temperature, the lowest minimum and the highest maximum, so extremes are never lost.
/// </summary>
internal static class BucketAverageDownsampler
{
    /// <summary>Data is ingested once per minute, so finer buckets would add nothing.</summary>
    internal static readonly TimeSpan MinimumBucketWidth = TimeSpan.FromMinutes(1);

    public static IReadOnlyList<TemperatureSample> Downsample(
        IReadOnlyList<TemperatureSample> samples,
        DateRange range,
        int maxPoints)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxPoints, 1);

        var start = range.FromUtc.Ticks;
        var span = range.ToUtc.Ticks - start;

        // "+ 1" keeps a sample exactly at ToUtc inside the last bucket, so at most maxPoints buckets exist.
        var bucketWidth = Math.Max(MinimumBucketWidth.Ticks, (long)Math.Ceiling((span + 1) / (double)maxPoints));

        return
        [
            .. samples
                .GroupBy(s => Math.Max(0, s.TimestampUtc.Ticks - start) / bucketWidth)
                .OrderBy(g => g.Key)
                .Select(g => new TemperatureSample(
                    new DateTime(start + (g.Key * bucketWidth), DateTimeKind.Utc),
                    Math.Round(g.Average(s => s.Temperature), 2),
                    g.Min(s => s.TemperatureMin),
                    g.Max(s => s.TemperatureMax))),
        ];
    }
}
