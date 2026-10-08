using System.Text.Json;
using CloudReports.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace CloudReports.Infrastructure.Weather;

/// <summary>Calls the OpenWeatherMap "current weather" endpoint (<c>/data/2.5/weather</c>).</summary>
internal sealed class OpenWeatherMapProvider(HttpClient httpClient, IOptions<OpenWeatherMapOptions> options)
    : IWeatherProvider
{
    public async Task<WeatherFetchResult> GetCurrentWeatherAsync(string city, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(city);

        var settings = options.Value;
        var requestUri = $"data/2.5/weather?q={Uri.EscapeDataString(city)}" +
                         $"&appid={Uri.EscapeDataString(settings.ApiKey)}" +
                         $"&units={Uri.EscapeDataString(settings.Units)}";

        try
        {
            using var response = await httpClient.GetAsync(requestUri, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var statusCode = (int)response.StatusCode;

            return response.IsSuccessStatusCode
                ? Parse(body, statusCode)
                : WeatherFetchResult.Failure(DescribeError(body, response.ReasonPhrase), statusCode);
        }
        catch (HttpRequestException ex)
        {
            return WeatherFetchResult.Failure($"Request failed: {ex.Message}");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return WeatherFetchResult.Failure($"Request timed out after {settings.TimeoutSeconds}s.");
        }
    }

    private static WeatherFetchResult Parse(string body, int statusCode)
    {
        OpenWeatherMapResponse? payload;
        try
        {
            payload = JsonSerializer.Deserialize<OpenWeatherMapResponse>(body);
        }
        catch (JsonException ex)
        {
            return WeatherFetchResult.Failure($"Invalid JSON payload: {ex.Message}", statusCode);
        }

        if (payload is not { Name: { Length: > 0 } name, Dt: { } dt, Main: { } main })
        {
            return WeatherFetchResult.Failure("Payload is missing required fields (name, dt, main).", statusCode);
        }

        var observation = new WeatherObservation(
            name,
            payload.Sys?.Country ?? string.Empty,
            main.Temp,
            main.TempMin,
            main.TempMax,
            DateTimeOffset.FromUnixTimeSeconds(dt).UtcDateTime);

        return WeatherFetchResult.Success(statusCode, body, observation);
    }

    /// <summary>
    /// Uses the provider's error message when the body has the documented shape; otherwise only the HTTP status
    /// text, so arbitrary bodies (e.g. a proxy's HTML error page) never end up in the logs.
    /// </summary>
    private static string DescribeError(string body, string? reasonPhrase)
    {
        try
        {
            var error = JsonSerializer.Deserialize<OpenWeatherMapError>(body);
            if (!string.IsNullOrWhiteSpace(error?.Message))
            {
                return error.Message;
            }
        }
        catch (JsonException)
        {
            // Fall through: body is not the documented error shape.
        }

        return reasonPhrase ?? "Unknown error";
    }
}
