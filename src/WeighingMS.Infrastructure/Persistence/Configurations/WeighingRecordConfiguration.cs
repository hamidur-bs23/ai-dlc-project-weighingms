using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeighingMS.Domain.Common;
using WeighingMS.Domain.Weighing;

namespace WeighingMS.Infrastructure.Persistence.Configurations;

/// <summary>
/// Table <c>weighing_records</c>: immutable confirmed weighings (FR-19, STEP 25). One per component (unique component and
/// unique session + sequence); quantities <c>numeric(12,4)</c> with units. Changes are refused by the DbContext (A9).
/// </summary>
public sealed class WeighingRecordConfiguration : IEntityTypeConfiguration<WeighingRecord>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<WeighingRecord> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("weighing_records");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property<Guid>("ComponentId");
        builder.HasIndex("ComponentId").IsUnique();
        builder.HasOne<WeighingSession>().WithMany().HasForeignKey(r => r.SessionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(r => new { r.SessionId, r.Sequence }).IsUnique();
        builder.HasIndex(r => r.BatchId);
        builder.Property(r => r.BatchId).HasMaxLength(32).IsRequired();
        builder.Property(r => r.ChemicalId).HasMaxLength(32).IsRequired();
        builder.Property(r => r.ChemicalName).HasMaxLength(100).IsRequired();
        builder.Property(r => r.Class).HasConversion<string>().HasMaxLength(16);
        builder.Property(r => r.ToleranceSource).HasConversion<string>().HasMaxLength(16);
        builder.Property(r => r.ScaleId).HasMaxLength(32).IsRequired();
        builder.Property(r => r.ScaleClass).HasConversion<string>().HasMaxLength(16);
        builder.Property(r => r.Lot).HasMaxLength(32).IsRequired();
        builder.Property(r => r.Container).HasMaxLength(32).IsRequired();
        builder.Property(r => r.OperatorBadgeId).HasMaxLength(32).IsRequired();
        builder.Ignore(r => r.Required);
        builder.Ignore(r => r.Actual);
        builder.Ignore(r => r.Tolerance);
        builder.Ignore(r => r.Tare);
        Quantity(builder, "_requiredValue", "_requiredUnit", "required");
        Quantity(builder, "_actualValue", "_actualUnit", "actual");
        Quantity(builder, "_toleranceValue", "_toleranceUnit", "tolerance");
        Quantity(builder, "_tareValue", "_tareUnit", "tare");
    }

    private static void Quantity(EntityTypeBuilder<WeighingRecord> builder, string valueField, string unitField, string column)
    {
        builder.Property<decimal>(valueField).HasColumnName($"{column}_value").HasPrecision(12, 4);
        builder.Property<MassUnit>(unitField).HasColumnName($"{column}_unit").HasConversion<string>().HasMaxLength(16);
    }
}
