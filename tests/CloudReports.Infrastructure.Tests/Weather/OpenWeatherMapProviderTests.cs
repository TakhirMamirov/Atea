using System.Net;
using System.Text;
using CloudReports.Infrastructure.Weather;
using Microsoft.Extensions.Options;

namespace CloudReports.Infrastructure.Tests.Weather;

public sealed class OpenWeatherMapProviderTests
{
    private const string LondonPayload = """
        {
          "coord": { "lon": -0.1257, "lat": 51.5085 },
          "weather": [ { "id": 804, "main": "Clouds", "description": "overcast clouds", "icon": "04d" } ],
          "main": { "temp": 14.2, "feels_like": 13.6, "temp_min": 12.9, "temp_max": 15.1, "pressure": 1012, "humidity": 77 },
          "dt": 1791374400,
          "sys": { "country": "GB", "sunrise": 1791354000, "sunset": 1791394000 },
          "name": "London",
          "cod": 200
        }
        """;

    [Fact]
    public async Task GetCurrentWeatherAsync_Success_ParsesObservationAndKeepsFullPayload()
    {
        var handler = new StubHandler(HttpStatusCode.OK, LondonPayload);
        var sut = CreateSut(handler);

        var result = await sut.GetCurrentWeatherAsync("London", TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(200, result.HttpStatusCode);
        Assert.Equal(LondonPayload, result.RawPayload);
        var observation = Assert.IsType<Application.Abstractions.WeatherObservation>(result.Observation);
        Assert.Equal("London", observation.City);
        Assert.Equal("GB", observation.Country);
        Assert.Equal(14.2, observation.Temperature);
        Assert.Equal(12.9, observation.TemperatureMin);
        Assert.Equal(15.1, observation.TemperatureMax);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1791374400).UtcDateTime, observation.ObservedAtUtc);
    }

    [Fact]
    public async Task GetCurrentWeatherAsync_BuildsDocumentedRequestUri()
    {
        var handler = new StubHandler(HttpStatusCode.OK, LondonPayload);
        var sut = CreateSut(handler);

        await sut.GetCurrentWeatherAsync("Rome", TestContext.Current.CancellationToken);

        Assert.Equal(
            "https://api.example.test/data/2.5/weather?q=Rome&appid=test-key&units=metric",
            handler.LastRequestUri?.ToString());
    }

    [Fact]
    public async Task GetCurrentWeatherAsync_Unauthorized_ReturnsFailureWithProviderMessage()
    {
        var handler = new StubHandler(HttpStatusCode.Unauthorized, """{"cod":401,"message":"Invalid API key."}""");
        var sut = CreateSut(handler);

        var result = await sut.GetCurrentWeatherAsync("Riga", TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(401, result.HttpStatusCode);
        Assert.Equal("Invalid API key.", result.Error);
        Assert.Null(result.RawPayload);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("""{"cod":200}""")]
    public async Task GetCurrentWeatherAsync_UnusablePayload_ReturnsFailure(string body)
    {
        var sut = CreateSut(new StubHandler(HttpStatusCode.OK, body));

        var result = await sut.GetCurrentWeatherAsync("Riga", TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(200, result.HttpStatusCode);
        Assert.False(string.IsNullOrWhiteSpace(result.Error));
    }

    [Fact]
    public async Task GetCurrentWeatherAsync_NetworkError_ReturnsFailureWithoutStatusCode()
    {
        var sut = CreateSut(new StubHandler(new HttpRequestException("No such host is known.")));

        var result = await sut.GetCurrentWeatherAsync("Riga", TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Null(result.HttpStatusCode);
        Assert.Contains("No such host", result.Error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetCurrentWeatherAsync_Timeout_ReturnsFailure()
    {
        var sut = CreateSut(new StubHandler(new TaskCanceledException("timeout")));

        var result = await sut.GetCurrentWeatherAsync("Riga", TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Contains("timed out", result.Error, StringComparison.Ordinal);
    }

    private static OpenWeatherMapProvider CreateSut(StubHandler handler)
    {
        var options = new OpenWeatherMapOptions { BaseUrl = new Uri("https://api.example.test/"), ApiKey = "test-key" };
        var client = new HttpClient(handler) { BaseAddress = options.BaseUrl };
        return new OpenWeatherMapProvider(client, Options.Create(options));
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _body = string.Empty;
        private readonly Exception? _exception;

        public StubHandler(HttpStatusCode statusCode, string body)
        {
            _statusCode = statusCode;
            _body = body;
        }

        public StubHandler(Exception exception) => _exception = exception;

        public Uri? LastRequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;

            if (_exception is not null)
            {
                return Task.FromException<HttpResponseMessage>(_exception);
            }

            return Task.FromResult(new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_body, Encoding.UTF8, "application/json"),
            });
        }
    }
}
