using CloudReports.Application.Logs;

namespace CloudReports.Web.Models;

/// <summary>Query-string parameters of <c>GET /api/fetch-logs</c>.</summary>
public sealed record FetchLogsRequest
{
    /// <summary>Only logs for this city (e.g. "Riga"). Omit for all cities.</summary>
    public string? City { get; init; }

    /// <summary><c>true</c> for successful attempts, <c>false</c> for failures. Omit for both.</summary>
    public bool? IsSuccess { get; init; }

    /// <summary>1-based page number. Values below 1 are treated as 1.</summary>
    public int Page { get; init; } = 1;

    /// <summary>Entries per page (default 20). Values are clamped to 1–200.</summary>
    public int PageSize { get; init; } = FetchLogQuery.DefaultPageSize;

    /// <summary>Converts the request into the application-layer query.</summary>
    public FetchLogQuery ToQuery() => new(Page, PageSize, City, IsSuccess);
}
