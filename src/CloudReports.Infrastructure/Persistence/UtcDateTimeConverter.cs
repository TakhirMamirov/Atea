using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CloudReports.Infrastructure.Persistence;

/// <summary>
/// All timestamps are stored as UTC. Writing a non-UTC value fails loudly instead of being silently converted
/// using the server's time zone; values read back are marked as UTC.
/// </summary>
internal sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    value => EnsureUtc(value),
    value => DateTime.SpecifyKind(value, DateTimeKind.Utc))
{
    private static DateTime EnsureUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc
            ? value
            : throw new ArgumentException($"Only UTC timestamps can be stored, but got DateTimeKind.{value.Kind}.", nameof(value));
}
