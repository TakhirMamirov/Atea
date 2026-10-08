using CloudReports.Application.Ingestion;
using CloudReports.Infrastructure.Weather;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CloudReports.Functions.Tests;

public sealed class CompositionTests
{
    private static readonly Dictionary<string, string?> ValidSettings = new()
    {
        ["ConnectionStrings:CloudReports"] = "Server=localhost;Database=CloudReports;TrustServerCertificate=True",
        ["OpenWeatherMap:ApiKey"] = "test-key",
        ["WeatherIngestion:Cities:0"] = "London",
        ["WeatherIngestion:Cities:1"] = "Rome",
        ["WeatherIngestion:Cities:2"] = "Riga",
    };

    [Fact]
    public void AddWeatherIngestionWorker_ResolvesIngestionPipelineFromAppSettings()
    {
        using var provider = BuildProvider(ValidSettings);
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<WeatherIngestionFunction>());
        Assert.Equal(
            ["London", "Rome", "Riga"],
            scope.ServiceProvider.GetRequiredService<IOptions<WeatherIngestionOptions>>().Value.Cities);
    }

    [Fact]
    public void AddWeatherIngestionWorker_MissingApiKey_FailsOptionsValidation()
    {
        var settings = new Dictionary<string, string?>(ValidSettings) { ["OpenWeatherMap:ApiKey"] = "" };
        using var provider = BuildProvider(settings);

        Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<OpenWeatherMapOptions>>().Value);
    }

    private static ServiceProvider BuildProvider(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection().AddLogging();
        services.AddWeatherIngestionWorker(configuration);
        services.AddScoped<WeatherIngestionFunction>();
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }
}
