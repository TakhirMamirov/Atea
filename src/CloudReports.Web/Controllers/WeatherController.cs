using CloudReports.Application.Common;
using CloudReports.Application.Reports;
using Microsoft.AspNetCore.Mvc;

namespace CloudReports.Web.Controllers;

/// <summary>Weather data for the chart and city summary on the weather page.</summary>
[ApiController]
[Route("api/weather")]
[Produces("application/json")]
public sealed class WeatherController(IWeatherReportService reportService, TimeProvider timeProvider) : ControllerBase
{
    /// <summary>
    /// Returns temperature series and per-city summaries for the given range.
    /// Defaults to the last 24 hours when bounds are omitted.
    /// </summary>
    /// <param name="from">Start of the range (ISO 8601). Defaults to 24 hours before <paramref name="to"/>.</param>
    /// <param name="to">End of the range (ISO 8601). Defaults to now.</param>
    /// <param name="cancellationToken">Cancelled when the client disconnects.</param>
    /// <response code="200">The report; series and cities are empty when there is no data in the range.</response>
    /// <response code="400">
    /// A date can't be parsed, <paramref name="from"/> is not earlier than <paramref name="to"/>,
    /// or the range is longer than 31 days.
    /// </response>
    /// <response code="500">Unexpected server error, e.g. the database is unavailable.</response>
    [HttpGet]
    [ProducesResponseType<WeatherReport>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<WeatherReport>> GetReport(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        if (!DateRange.TryCreate(from, to, timeProvider.GetUtcNow().UtcDateTime, out var range, out var error))
        {
            ModelState.AddModelError(nameof(from), error);
            return ValidationProblem(ModelState);
        }

        return await reportService.GetReportAsync(range, cancellationToken);
    }
}
