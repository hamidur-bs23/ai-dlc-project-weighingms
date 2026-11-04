using WeighingMS.Application.Abstractions;
using WeighingMS.Domain.Audit;
using WeighingMS.Infrastructure.Persistence;

namespace WeighingMS.Infrastructure.Access;

/// <summary>Adds audit entries to the current DbContext, so they commit with the action they record.</summary>
public sealed class AuditTrail(WeighingDbContext db) : IAuditTrail
{
    /// <inheritdoc />
    public void Append(AuditEntry entry) => db.AuditEntries.Add(entry);
}
