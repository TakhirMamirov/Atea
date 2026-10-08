using CloudReports.Application.Common;
using CloudReports.Application.Logs;
using CloudReports.Domain.Entities;

namespace CloudReports.Application.Abstractions;

/// <summary>Stores and queries fetch attempt logs.</summary>
public interface IFetchLogRepository
{
    /// <summary>Adds a log entry; it is saved on the next <see cref="IUnitOfWork.SaveChangesAsync"/>.</summary>
    void Add(FetchLog log);

    /// <summary>Returns a page of logs matching the query, newest first.</summary>
    Task<PagedResult<FetchLog>> GetPageAsync(FetchLogQuery query, CancellationToken cancellationToken);
}
