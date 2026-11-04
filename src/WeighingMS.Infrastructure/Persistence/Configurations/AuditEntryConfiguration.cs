using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeighingMS.Domain.Audit;

namespace WeighingMS.Infrastructure.Persistence.Configurations;

/// <summary>Table <c>audit_entries</c> (append-only).</summary>
public sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("audit_entries");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.Type).HasConversion<string>().HasMaxLength(32);
        builder.Property(a => a.ActorBadgeId).HasMaxLength(32).IsRequired();
        builder.Property(a => a.Subject).HasMaxLength(100);
        builder.Property(a => a.Reason).HasMaxLength(500);
        builder.HasIndex(a => a.OccurredAt);
    }
}
