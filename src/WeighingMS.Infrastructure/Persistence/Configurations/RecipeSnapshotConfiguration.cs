using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeighingMS.Domain.Batches;

namespace WeighingMS.Infrastructure.Persistence.Configurations;

/// <summary>Table <c>recipe_snapshots</c>: one per batch (unique batch ID).</summary>
public sealed class RecipeSnapshotConfiguration : IEntityTypeConfiguration<RecipeSnapshot>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RecipeSnapshot> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("recipe_snapshots");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.BatchId).HasMaxLength(32).IsRequired();
        builder.HasIndex(s => s.BatchId).IsUnique();
        builder.Property(s => s.OrderId).HasMaxLength(32).IsRequired();
        builder.Property(s => s.Machine).HasMaxLength(32).IsRequired();
        builder.Property(s => s.Fabric).HasMaxLength(100).IsRequired();
        builder.Property(s => s.Shade).HasMaxLength(64).IsRequired();
        builder.Property(s => s.BatchWeightKg).HasPrecision(12, 4);
        builder.Ignore(s => s.Components);
        builder.HasMany<SnapshotComponent>("_components").WithOne().HasForeignKey("RecipeSnapshotId").OnDelete(DeleteBehavior.Cascade);
        builder.Navigation("_components").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
