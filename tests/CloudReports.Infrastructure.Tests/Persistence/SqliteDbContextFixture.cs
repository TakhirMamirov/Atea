using CloudReports.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CloudReports.Infrastructure.Tests.Persistence;

/// <summary>Creates an isolated in-memory SQLite database per test, using the real EF Core model.</summary>
internal sealed class SqliteDbContextFixture : IAsyncDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    private SqliteDbContextFixture() { }

    public static async Task<SqliteDbContextFixture> CreateAsync()
    {
        var fixture = new SqliteDbContextFixture();
        await fixture._connection.OpenAsync();
        await using var context = fixture.CreateContext();
        await context.Database.EnsureCreatedAsync();
        return fixture;
    }

    public CloudReportsDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<CloudReportsDbContext>().UseSqlite(_connection).Options);

    public ValueTask DisposeAsync() => _connection.DisposeAsync();
}
