using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeighingMS.Domain.WriteBack;

namespace WeighingMS.Infrastructure.Persistence.Configurations;

/// <summary>
/// Table <c>orgatex_write_backs</c>: the store-and-forward outbox (PD-01). One per batch and one idempotency key per
/// row (both unique); the lines are <c>jsonb</c>; <c>xmin</c> concurrency between the dispatcher and a manual retry.
/// </summary>
public sealed class ConsumptionWriteBackConfiguration : IEntityTypeConfiguration<ConsumptionWriteBack>
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ConsumptionWriteBack> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("orgatex_write_backs");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).ValueGeneratedNever();
        builder.Property(w => w.BatchId).HasMaxLength(32).IsRequired();
        builder.HasIndex(w => w.BatchId).IsUnique();
        builder.HasIndex(w => w.IdempotencyKey).IsUnique();
        builder.HasIndex(w => new { w.Status, w.NextAttemptAt });
        builder.Property(w => w.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(w => w.LastErrorCode).HasMaxLength(100);
        builder.Property<uint>("xmin").IsRowVersion();
        builder.Ignore(w => w.Lines);
        builder.Property<List<ConsumptionLine>>("_lines")
            .HasColumnName("lines")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, _json),
                v => JsonSerializer.Deserialize<List<ConsumptionLine>>(v, _json) ?? new List<ConsumptionLine>(),
                new ValueComparer<List<ConsumptionLine>>(
                    (a, b) => (a ?? new List<ConsumptionLine>()).SequenceEqual(b ?? new List<ConsumptionLine>()),
                    v => v.Aggregate(0, (h, l) => HashCode.Combine(h, l.GetHashCode())),
                    v => v.ToList()));
    }
}
