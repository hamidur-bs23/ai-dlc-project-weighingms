using Microsoft.EntityFrameworkCore;
using WeighingMS.Application.Abstractions;
using WeighingMS.Domain.Access;
using WeighingMS.Domain.Audit;
using WeighingMS.Domain.Batches;
using WeighingMS.Domain.Weighing;

namespace WeighingMS.Infrastructure.Persistence;

/// <summary>
/// WeighingMS operational data in PostgreSQL 17 (data-stack). Snake_case names; one configuration class per
/// aggregate; the domain types stay persistence-ignorant. Also the unit of work for the Application layer.
/// </summary>
public sealed class WeighingDbContext(DbContextOptions<WeighingDbContext> options) : DbContext(options), IUnitOfWork
{
    /// <summary>Operators and supervisors.</summary>
    public DbSet<StationUser> StationUsers => Set<StationUser>();

    /// <summary>Supervisor step-up grants.</summary>
    public DbSet<StepUpGrant> StepUpGrants => Set<StepUpGrant>();

    /// <summary>Recipe snapshots (one per batch, immutable).</summary>
    public DbSet<RecipeSnapshot> RecipeSnapshots => Set<RecipeSnapshot>();

    /// <summary>Weighing sessions (one per batch).</summary>
    public DbSet<WeighingSession> WeighingSessions => Set<WeighingSession>();

    /// <summary>Confirmed weighing records (immutable).</summary>
    public DbSet<WeighingRecord> WeighingRecords => Set<WeighingRecord>();

    /// <summary>Append-only audit trail.</summary>
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    /// <inheritdoc />
    async Task IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken) => await SaveChangesAsync(cancellationToken);

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        RejectAuditChanges();
        RejectRecordChanges();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WeighingDbContext).Assembly);
    }

    /// <summary>Weighing records are immutable (FR-19, A9): modifying or deleting one is a programming error.</summary>
    private void RejectRecordChanges()
    {
        var tampered = ChangeTracker.Entries<WeighingRecord>()
            .Any(e => e.State is EntityState.Modified or EntityState.Deleted);
        if (tampered)
        {
            throw new InvalidOperationException("Weighing records are immutable and cannot be modified or deleted.");
        }
    }

    /// <summary>The audit trail is append-only: modifying or deleting an entry is a programming error.</summary>
    private void RejectAuditChanges()
    {
        var tampered = ChangeTracker.Entries<AuditEntry>()
            .Any(e => e.State is EntityState.Modified or EntityState.Deleted);
        if (tampered)
        {
            throw new InvalidOperationException("Audit entries are append-only and cannot be modified or deleted.");
        }
    }
}
