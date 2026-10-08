namespace CloudReports.Application.Logs;

public sealed record FetchLogDto(
    long Id,
    string City,
    DateTime AttemptedAtUtc,
    bool IsSuccess,
    int? HttpStatusCode,
    long DurationMs,
    string? ErrorMessage);
