using CloudReports.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudReports.Infrastructure.Persistence.Configurations;

internal sealed class WeatherReadingConfiguration : IEntityTypeConfiguration<WeatherReading>
{
    public void Configure(EntityTypeBuilder<WeatherReading> builder)
    {
        builder.ToTable("WeatherReadings");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.City).HasMaxLength(100).IsRequired();
        builder.Property(r => r.Country).HasMaxLength(10).IsRequired();
        builder.Property(r => r.RawPayload).IsRequired();

        // Covers the chart query: a range scan on fetch time that reads every needed column from the index,
        // without touching the table rows (and their raw payloads).
        builder.HasIndex(r => new { r.FetchedAtUtc, r.City })
            .IncludeProperties(r => new { r.Country, r.ObservedAtUtc, r.Temperature, r.TemperatureMin, r.TemperatureMax });
    }
}
