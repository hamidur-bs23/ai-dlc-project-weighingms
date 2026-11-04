namespace WeighingMS.Application.Abstractions;

/// <summary>Commits the changes of one use case atomically (action + audit in the same transaction).</summary>
public interface IUnitOfWork
{
    /// <summary>Saves all pending changes.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
