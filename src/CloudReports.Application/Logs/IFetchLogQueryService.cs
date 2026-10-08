using CloudReports.Application.Common;

namespace CloudReports.Application.Logs;

/// <summary>Provides fetch attempt logs for the logs page.</summary>
public interface IFetchLogQueryService
{
    /// <summary>Returns a page of logs, newest first. Out-of-range paging values are clamped.</summary>
    Task<PagedResult<FetchLogDto>> GetLogsAsync(FetchLogQuery query, CancellationToken cancellationToken);
}
