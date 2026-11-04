using WeighingMS.Domain.Access;

namespace WeighingMS.Application.Access;

/// <summary>Issued supervisor step-up grants.</summary>
public interface IStepUpGrantStore
{
    /// <summary>Adds a new grant to the current unit of work.</summary>
    void Add(StepUpGrant grant);

    /// <summary>Finds a grant by id, tracked for changes.</summary>
    Task<StepUpGrant?> FindAsync(Guid grantId, CancellationToken cancellationToken);
}
