using CloudReports.Functions;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.Services.AddWeatherIngestionWorker(builder.Configuration);

await builder.Build().RunAsync();
