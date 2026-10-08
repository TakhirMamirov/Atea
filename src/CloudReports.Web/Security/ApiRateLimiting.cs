using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;

namespace CloudReports.Web.Security;

/// <summary>
/// Limits API requests per client IP, so a burst of expensive queries can't overload the small database.
/// Limits are configurable via <c>RateLimiting:PermitLimit</c> and <c>RateLimiting:WindowSeconds</c>.
/// </summary>
internal static class ApiRateLimiting
{
    public const string PolicyName = "api";

    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var permitLimit = configuration.GetValue("RateLimiting:PermitLimit", 60);
        var window = TimeSpan.FromSeconds(configuration.GetValue("RateLimiting:WindowSeconds", 60));

        // Behind Azure App Service the client IP arrives in X-Forwarded-For. Only the last entry is used
        // (the one the platform appends), so clients can't spoof their IP to escape the limit.
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(PolicyName, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = permitLimit, Window = window }));
        });

        return services;
    }
}
