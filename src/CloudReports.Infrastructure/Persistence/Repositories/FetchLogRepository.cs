using CloudReports.Application.Abstractions;
using CloudReports.Application.Common;
using CloudReports.Application.Logs;
using CloudReports.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CloudReports.Infrastructure.Persistence.Repositories;

internal sealed class FetchLogRepository(CloudReportsDbContext dbContext) : IFetchLogRepository
{
    public void Add(FetchLog log) => dbContext.FetchLogs.Add(log);

    public async Task<PagedResult<FetchLog>> GetPageAsync(FetchLogQuery query, CancellationToken cancellationToken)
    {
        var logs = dbContext.FetchLogs.AsNoTracking();

        if (query.City is not null)
        {
            logs = logs.Where(l => l.City == query.City);
        }

        if (query.IsSuccess is { } isSuccess)
        {
            logs = logs.Where(l => l.IsSuccess == isSuccess);
        }

        var totalCount = await logs.CountAsync(cancellationToken);
        var items = await logs
            .OrderByDescending(l => l.AttemptedAtUtc)
            .ThenByDescending(l => l.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<FetchLog>(items, totalCount, query.Page, query.PageSize);
    }
}
