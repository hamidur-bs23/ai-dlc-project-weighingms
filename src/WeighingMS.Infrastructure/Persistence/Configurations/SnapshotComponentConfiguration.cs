using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeighingMS.Domain.Batches;
using WeighingMS.Domain.Common;

namespace WeighingMS.Infrastructure.Persistence.Configurations;

/// <summary>
/// Table <c>recipe_snapshot_components</c>. Quantities are <c>numeric(12,4)</c> with the unit stored explicitly
/// (data-stack); the domain exposes them as <see cref="Quantity"/>.
/// </summary>
public sealed class SnapshotComponentConfiguration : IEntityTypeConfiguration<SnapshotComponent>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SnapshotComponent> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("recipe_snapshot_components");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property<Guid>("RecipeSnapshotId");
        builder.HasIndex("RecipeSnapshotId", nameof(SnapshotComponent.Sequence)).IsUnique();
        builder.Property(c => c.ChemicalId).HasMaxLength(32).IsRequired();
        builder.Property(c => c.Code).HasMaxLength(32).IsRequired();
        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        builder.Property(c => c.DisplayColour).HasMaxLength(16).IsRequired();
        builder.Property(c => c.SuggestedContainer).HasMaxLength(32);
        builder.Ignore(c => c.Target);
        builder.Ignore(c => c.RecipeTolerance);
        builder.Property<decimal>("_targetValue").HasColumnName("target_value").HasPrecision(12, 4);
        builder.Property<MassUnit>("_targetUnit").HasColumnName("target_unit").HasConversion<string>().HasMaxLength(16);
        builder.Property<decimal?>("_toleranceValue").HasColumnName("tolerance_value").HasPrecision(12, 4);
    }
}
