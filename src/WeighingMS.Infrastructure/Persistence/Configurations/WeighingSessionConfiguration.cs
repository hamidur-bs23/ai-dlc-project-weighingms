using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeighingMS.Domain.Weighing;

namespace WeighingMS.Infrastructure.Persistence.Configurations;

/// <summary>Table <c>weighing_sessions</c>: one per batch (unique batch ID), <c>xmin</c> optimistic concurrency (A2).</summary>
public sealed class WeighingSessionConfiguration : IEntityTypeConfiguration<WeighingSession>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<WeighingSession> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("weighing_sessions");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.BatchId).HasMaxLength(32).IsRequired();
        builder.HasIndex(s => s.BatchId).IsUnique();
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(s => s.StartedBy).HasMaxLength(32).IsRequired();
        builder.Property<uint>("xmin").IsRowVersion();
        builder.Ignore(s => s.Components);
        builder.Ignore(s => s.Active);
        builder.Ignore(s => s.Suggested);
        builder.Ignore(s => s.DoneCount);
        builder.Ignore(s => s.Events);
        builder.HasMany<ComponentWeighing>("_components").WithOne().HasForeignKey("WeighingSessionId").OnDelete(DeleteBehavior.Cascade);
        builder.Navigation("_components").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
