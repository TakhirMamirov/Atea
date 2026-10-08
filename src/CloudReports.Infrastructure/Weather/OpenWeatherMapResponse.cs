using System.Text.Json.Serialization;

namespace CloudReports.Infrastructure.Weather;

/// <summary>Subset of the OpenWeatherMap "current weather" response needed for reporting.</summary>
internal sealed record OpenWeatherMapResponse(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("dt")] long? Dt,
    [property: JsonPropertyName("main")] OpenWeatherMapMain? Main,
    [property: JsonPropertyName("sys")] OpenWeatherMapSys? Sys);

internal sealed record OpenWeatherMapMain(
    [property: JsonPropertyName("temp")] double Temp,
    [property: JsonPropertyName("temp_min")] double TempMin,
    [property: JsonPropertyName("temp_max")] double TempMax);

internal sealed record OpenWeatherMapSys(
    [property: JsonPropertyName("country")] string? Country);

/// <summary>Error body returned by OpenWeatherMap for non-success responses.</summary>
internal sealed record OpenWeatherMapError(
    [property: JsonPropertyName("message")] string? Message);
