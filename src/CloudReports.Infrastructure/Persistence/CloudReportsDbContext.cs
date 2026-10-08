using CloudReports.Application.Abstractions;
using CloudReports.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CloudReports.Infrastructure.Persistence;

public sealed class CloudReportsDbContext(DbContextOptions<CloudReportsDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<WeatherReading> WeatherReadings => Set<WeatherReading>();

    public DbSet<FetchLog> FetchLogs => Set<FetchLog>();

    async Task IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken) =>
        await SaveChangesAsync(cancellationToken);

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CloudReportsDbContext).Assembly);

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // All timestamps are stored as UTC; restore DateTimeKind.Utc when materializing.
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }
}
