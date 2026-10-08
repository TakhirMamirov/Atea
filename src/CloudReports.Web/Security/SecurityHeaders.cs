namespace CloudReports.Web.Security;

/// <summary>Adds standard browser security headers to every response, including error responses.</summary>
internal static class SecurityHeaders
{
    // Scripts, styles and data only from this site. Inline style attributes are allowed because the chart
    // library sets them; inline scripts are not.
    private const string ContentSecurityPolicy =
        "default-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; " +
        "frame-ancestors 'none'; base-uri 'self'; form-action 'self'";

    /// <remarks>
    /// Register first. The headers are written when the response starts, so they survive the exception handler
    /// clearing the response to write a 500 error.
    /// </remarks>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.Use((context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;
                headers.ContentSecurityPolicy = ContentSecurityPolicy;
                headers.XContentTypeOptions = "nosniff";
                headers.XFrameOptions = "DENY";
                headers["Referrer-Policy"] = "no-referrer";
                return Task.CompletedTask;
            });
            return next(context);
        });
}
