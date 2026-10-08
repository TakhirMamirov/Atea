using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CloudReports.Infrastructure.Persistence;

/// <summary>Used only by <c>dotnet ef</c> tooling to create migrations without a host.</summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<CloudReportsDbContext>
{
    public CloudReportsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CloudReportsDbContext>()
            .UseSqlServer("Server=localhost;Database=CloudReports;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        return new CloudReportsDbContext(options);
    }
}
