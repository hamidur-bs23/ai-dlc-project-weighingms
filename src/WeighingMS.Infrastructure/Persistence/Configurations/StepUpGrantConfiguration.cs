using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeighingMS.Domain.Access;

namespace WeighingMS.Infrastructure.Persistence.Configurations;

/// <summary>Table <c>step_up_grants</c>. <c>xmin</c> concurrency prevents a grant being used twice in parallel.</summary>
public sealed class StepUpGrantConfiguration : IEntityTypeConfiguration<StepUpGrant>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<StepUpGrant> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("step_up_grants");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).ValueGeneratedNever();
        builder.Property(g => g.SupervisorBadgeId).HasMaxLength(32).IsRequired();
        builder.Property(g => g.SupervisorName).HasMaxLength(100).IsRequired();
        builder.Property(g => g.Action).HasMaxLength(32).IsRequired();
        builder.HasOne<StationUser>().WithMany().HasForeignKey(g => g.SupervisorId).OnDelete(DeleteBehavior.Restrict);
        builder.Property<uint>("xmin").IsRowVersion();
    }
}
