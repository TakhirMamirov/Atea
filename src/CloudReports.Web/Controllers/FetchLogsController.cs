using CloudReports.Application.Common;
using CloudReports.Application.Logs;
using Microsoft.AspNetCore.Mvc;

namespace CloudReports.Web.Controllers;

/// <summary>Log of every attempt to fetch weather data, for the logs page.</summary>
[ApiController]
[Route("api/fetch-logs")]
[Produces("application/json")]
public sealed class FetchLogsController(IFetchLogQueryService logQueryService) : ControllerBase
{
    /// <summary>Returns a page of fetch attempt logs, newest first.</summary>
    /// <param name="city">Only logs for this city (e.g. "Riga"). Omit for all cities.</param>
    /// <param name="isSuccess"><c>true</c> for successful attempts, <c>false</c> for failures. Omit for both.</param>
    /// <param name="cancellationToken">Cancelled when the client disconnects.</param>
    /// <param name="page">1-based page number. Values below 1 are treated as 1.</param>
    /// <param name="pageSize">Entries per page (default 20). Values are clamped to 1–200.</param>
    /// <response code="200">The requested page; empty when nothing matches.</response>
    /// <response code="400">A parameter has an invalid format, e.g. <c>page=abc</c> or <c>isSuccess=maybe</c>.</response>
    /// <response code="500">Unexpected server error, e.g. the database is unavailable.</response>
    [HttpGet]
    [ProducesResponseType<PagedResult<FetchLogDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResult<FetchLogDto>>> GetLogs(
        [FromQuery] string? city,
        [FromQuery] bool? isSuccess,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = FetchLogQuery.DefaultPageSize)
    {
        var query = new FetchLogQuery(page, pageSize, city, isSuccess);
        return await logQueryService.GetLogsAsync(query, cancellationToken);
    }
}
