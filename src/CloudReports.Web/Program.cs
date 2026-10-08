using Azure.Monitor.OpenTelemetry.AspNetCore;
using CloudReports.Application;
using CloudReports.Infrastructure;
using CloudReports.Infrastructure.Persistence;
using CloudReports.Web.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddApiRateLimiting(builder.Configuration);

builder.Services
    .AddPersistence(builder.Configuration)
    .AddReporting();

builder.Services.AddHealthChecks().AddDbContextCheck<CloudReportsDbContext>();

if (!string.IsNullOrWhiteSpace(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
{
    builder.Services.AddOpenTelemetry().UseAzureMonitor();
}

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    await app.Services.MigrateDatabaseAsync();
}

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseHsts();
}

app.UseSecurityHeaders();

// Serves the compiled React app (copied into wwwroot at publish time).
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRateLimiter();

app.MapControllers().RequireRateLimiting(ApiRateLimiting.PolicyName);
app.MapHealthChecks("/health");

// Unknown API routes must return 404 instead of the SPA shell.
app.MapFallback("/api/{**path}", () => Results.Problem(statusCode: StatusCodes.Status404NotFound));
app.MapFallbackToFile("index.html");

await app.RunAsync();
