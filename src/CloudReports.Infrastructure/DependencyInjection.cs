using CloudReports.Application.Abstractions;
using CloudReports.Infrastructure.Persistence;
using CloudReports.Infrastructure.Persistence.Repositories;
using CloudReports.Infrastructure.Weather;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CloudReports.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "CloudReports";

    /// <summary>Registers the EF Core context, repositories and unit of work.</summary>
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured.");
        }

        services.AddDbContext<CloudReportsDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<CloudReportsDbContext>());
        services.AddScoped<IWeatherReadingRepository, WeatherReadingRepository>();
        services.AddScoped<IFetchLogRepository, FetchLogRepository>();
        return services;
    }

    /// <summary>Registers the OpenWeatherMap implementation of <see cref="IWeatherProvider"/>.</summary>
    public static IServiceCollection AddOpenWeatherMap(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<OpenWeatherMapOptions>()
            .Bind(configuration.GetSection(OpenWeatherMapOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<IWeatherProvider, OpenWeatherMapProvider>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<OpenWeatherMapOptions>>().Value;
            client.BaseAddress = options.BaseUrl;
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        return services;
    }

    /// <summary>Applies pending EF Core migrations.</summary>
    public static async Task MigrateDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CloudReportsDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}
