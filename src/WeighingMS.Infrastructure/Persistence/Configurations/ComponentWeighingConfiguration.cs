using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeighingMS.Domain.Common;
using WeighingMS.Domain.Weighing;

namespace WeighingMS.Infrastructure.Persistence.Configurations;

/// <summary>
/// Table <c>weighing_session_components</c>: the fixed scale and tolerance of each component (A1, SCALE-PROFILE).
/// Quantities are <c>numeric(12,4)</c> with the unit stored explicitly; a null tolerance means blocked (A3).
/// </summary>
public sealed class ComponentWeighingConfiguration : IEntityTypeConfiguration<ComponentWeighing>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ComponentWeighing> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("weighing_session_components");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property<Guid>("WeighingSessionId");
        builder.HasIndex("WeighingSessionId", nameof(ComponentWeighing.Sequence)).IsUnique();
        builder.Property(c => c.ChemicalId).HasMaxLength(32).IsRequired();
        builder.Property(c => c.Code).HasMaxLength(32).IsRequired();
        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        builder.Property(c => c.SuggestedContainer).HasMaxLength(32);
        builder.Property(c => c.ScaleId).HasMaxLength(32).IsRequired();
        builder.Property(c => c.ScaleClass).HasConversion<string>().HasMaxLength(16);
        builder.Property(c => c.ToleranceSource).HasConversion<string>().HasMaxLength(16);
        builder.Property(c => c.State).HasConversion<string>().HasMaxLength(16);
        builder.Ignore(c => c.Target);
        builder.Ignore(c => c.Scale);
        builder.Ignore(c => c.Tolerance);
        builder.Ignore(c => c.Band);
        builder.Ignore(c => c.Attempt);
        builder.Property<decimal>("_targetValue").HasColumnName("target_value").HasPrecision(12, 4);
        builder.Property<MassUnit>("_targetUnit").HasColumnName("target_unit").HasConversion<string>().HasMaxLength(16);
        builder.Property<MassUnit>("_scaleUnit").HasColumnName("scale_unit").HasConversion<string>().HasMaxLength(16);
        builder.Property<decimal>("_scaleResolution").HasColumnName("scale_resolution").HasPrecision(12, 4);
        builder.Property<decimal?>("_toleranceValue").HasColumnName("tolerance_value").HasPrecision(12, 4);
        builder.Property<MassUnit?>("_toleranceUnit").HasColumnName("tolerance_unit").HasConversion<string>().HasMaxLength(16);

        // The current attempt (bolt 008); all null when pending or done.
        builder.Property<DateTimeOffset?>("_attemptStartedAt").HasColumnName("attempt_started_at");
        builder.Property<string?>("_attemptStartedBy").HasColumnName("attempt_started_by").HasMaxLength(32);
        builder.Property<string?>("_lot").HasColumnName("lot").HasMaxLength(32);
        builder.Property<string?>("_container").HasColumnName("container").HasMaxLength(32);
        builder.Property<string?>("_tareScaleId").HasColumnName("tare_scale_id").HasMaxLength(32);
        builder.Property<decimal?>("_tareValue").HasColumnName("tare_value").HasPrecision(12, 4);
        builder.Property<MassUnit?>("_tareUnit").HasColumnName("tare_unit").HasConversion<string>().HasMaxLength(16);
        builder.Property<DateTimeOffset?>("_taredAt").HasColumnName("tared_at");

        // One record per component, at most (A8 backstop).
        builder.HasOne(c => c.Record).WithOne().HasForeignKey<WeighingRecord>("ComponentId").OnDelete(DeleteBehavior.Restrict);
    }
}
