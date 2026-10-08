using CloudReports.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudReports.Infrastructure.Persistence.Configurations;

internal sealed class FetchLogConfiguration : IEntityTypeConfiguration<FetchLog>
{
    public void Configure(EntityTypeBuilder<FetchLog> builder)
    {
        builder.ToTable("FetchLogs");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.City).HasMaxLength(100).IsRequired();
        builder.Property(l => l.ErrorMessage).HasMaxLength(FetchLog.MaxErrorMessageLength);

        builder.HasOne(l => l.WeatherReading)
            .WithMany()
            .HasForeignKey(l => l.WeatherReadingId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(l => l.AttemptedAtUtc);
        builder.HasIndex(l => new { l.City, l.AttemptedAtUtc });
    }
}
