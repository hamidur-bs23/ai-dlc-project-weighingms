using WeighingMS.Application.Access;
using WeighingMS.Domain.Access;
using WeighingMS.Infrastructure.Persistence;

namespace WeighingMS.Infrastructure.Access;

/// <summary>EF Core implementation of <see cref="IStepUpGrantStore"/>.</summary>
public sealed class StepUpGrantStore(WeighingDbContext db) : IStepUpGrantStore
{
    /// <inheritdoc />
    public void Add(StepUpGrant grant) => db.StepUpGrants.Add(grant);

    /// <inheritdoc />
    public async Task<StepUpGrant?> FindAsync(Guid grantId, CancellationToken cancellationToken) =>
        await db.StepUpGrants.FindAsync([grantId], cancellationToken);
}
