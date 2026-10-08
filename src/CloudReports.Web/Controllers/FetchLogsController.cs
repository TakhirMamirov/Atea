using CloudReports.Application.Common;
using CloudReports.Application.Logs;
using CloudReports.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CloudReports.Web.Controllers;

/// <summary>Log of every attempt to fetch weather data, for the logs page.</summary>
[ApiController]
[Route("api/fetch-logs")]
[Produces("application/json")]
public sealed class FetchLogsController(IFetchLogQueryService logQueryService) : ControllerBase
{
    /// <summary>Returns a page of fetch attempt logs, newest first.</summary>
    /// <param name="request">Filters and paging.</param>
    /// <param name="cancellationToken">Cancelled when the client disconnects.</param>
    /// <response code="200">The requested page; empty when nothing matches.</response>
    /// <response code="400">A parameter has an invalid format, e.g. <c>page=abc</c> or <c>isSuccess=maybe</c>.</response>
    /// <response code="429">Too many requests from this client; retry after a minute.</response>
    /// <response code="500">Unexpected server error, e.g. the database is unavailable.</response>
    [HttpGet]
    [ProducesResponseType<PagedResult<FetchLogDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResult<FetchLogDto>>> GetLogs(
        [FromQuery] FetchLogsRequest request,
        CancellationToken cancellationToken) =>
        await logQueryService.GetLogsAsync(request.ToQuery(), cancellationToken);
}
