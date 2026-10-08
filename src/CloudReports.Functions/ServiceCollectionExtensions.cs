using CloudReports.Application;
using CloudReports.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CloudReports.Functions;

public static class ServiceCollectionExtensions
{
    /// <summary>Composition root of the function app: everything the ingestion function depends on.</summary>
    public static IServiceCollection AddWeatherIngestionWorker(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);
        return services
            .AddPersistence(configuration)
            .AddOpenWeatherMap(configuration)
            .AddIngestion(configuration);
    }
}
