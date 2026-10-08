using CloudReports.Application.Ingestion;
using Microsoft.Azure.Functions.Worker;

namespace CloudReports.Functions.Tests;

public sealed class WeatherIngestionFunctionTests
{
    [Fact]
    public async Task Run_DelegatesToIngestionService()
    {
        var service = Substitute.For<IWeatherIngestionService>();
        var sut = new WeatherIngestionFunction(service);

        await sut.Run(new TimerInfo(), TestContext.Current.CancellationToken);

        await service.Received(1).IngestAsync(TestContext.Current.CancellationToken);
    }
}
