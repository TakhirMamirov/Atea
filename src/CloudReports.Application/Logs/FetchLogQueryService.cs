using CloudReports.Application.Abstractions;
using CloudReports.Application.Common;

namespace CloudReports.Application.Logs;

internal sealed class FetchLogQueryService(IFetchLogRepository fetchLogs) : IFetchLogQueryService
{
    public async Task<PagedResult<FetchLogDto>> GetLogsAsync(FetchLogQuery query, CancellationToken cancellationToken)
    {
        var page = await fetchLogs.GetPageAsync(query.Normalize(), cancellationToken);

        return page.Map(log => new FetchLogDto(
            log.Id,
            log.City,
            log.AttemptedAtUtc,
            log.IsSuccess,
            log.HttpStatusCode,
            log.DurationMs,
            log.ErrorMessage));
    }
}
