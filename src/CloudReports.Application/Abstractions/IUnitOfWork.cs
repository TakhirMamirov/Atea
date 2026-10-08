namespace CloudReports.Application.Abstractions;

/// <summary>Commits all pending repository changes atomically.</summary>
public interface IUnitOfWork
{
    /// <summary>Saves everything added to the repositories since the last save, in a single transaction.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
