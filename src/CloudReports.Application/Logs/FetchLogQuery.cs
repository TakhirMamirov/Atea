namespace CloudReports.Application.Logs;

public sealed record FetchLogQuery(
    int Page = 1,
    int PageSize = FetchLogQuery.DefaultPageSize,
    string? City = null,
    bool? IsSuccess = null)
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 200;

    /// <summary>Returns a copy with paging values clamped to valid bounds and blank filters removed.</summary>
    public FetchLogQuery Normalize() => this with
    {
        Page = Math.Max(1, Page),
        PageSize = Math.Clamp(PageSize, 1, MaxPageSize),
        City = string.IsNullOrWhiteSpace(City) ? null : City.Trim(),
    };
}
