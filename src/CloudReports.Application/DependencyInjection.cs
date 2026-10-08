using CloudReports.Application.Ingestion;
using CloudReports.Application.Logs;
using CloudReports.Application.Reports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CloudReports.Application;

public static class DependencyInjection
{
    /// <summary>Registers read-side services (reports and logs).</summary>
    public static IServiceCollection AddReporting(this IServiceCollection services)
    {
        services.AddScoped<IWeatherReportService, WeatherReportService>();
        services.AddScoped<IFetchLogQueryService, FetchLogQueryService>();
        return services;
    }

    /// <summary>Registers the write-side ingestion service and its options.</summary>
    public static IServiceCollection AddIngestion(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<WeatherIngestionOptions>()
            .Bind(configuration.GetSection(WeatherIngestionOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<IWeatherIngestionService, WeatherIngestionService>();
        return services;
    }
}
