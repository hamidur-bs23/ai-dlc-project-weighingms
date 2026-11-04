using WeighingMS.Domain.Audit;

namespace WeighingMS.Application.Abstractions;

/// <summary>Append-only audit trail. Entries are saved with the use case's unit of work.</summary>
public interface IAuditTrail
{
    /// <summary>Adds an entry to the current unit of work.</summary>
    void Append(AuditEntry entry);
}
