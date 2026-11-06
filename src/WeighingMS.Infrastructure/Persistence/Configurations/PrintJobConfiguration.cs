using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeighingMS.Domain.Tickets;

namespace WeighingMS.Infrastructure.Persistence.Configurations;

/// <summary>Table <c>print_jobs</c>: every print attempt of a ticket, successful or not.</summary>
public sealed class PrintJobConfiguration : IEntityTypeConfiguration<PrintJob>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PrintJob> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("print_jobs");
        builder.HasKey(j => j.Id);
        builder.Property(j => j.Id).ValueGeneratedNever();
        builder.Property(j => j.BatchId).HasMaxLength(32).IsRequired();
        builder.Property(j => j.OperatorBadgeId).HasMaxLength(32).IsRequired();
        builder.Property(j => j.ErrorCode).HasMaxLength(100);
        builder.HasIndex(j => j.TicketId);
        builder.HasOne<Ticket>().WithMany().HasForeignKey(j => j.TicketId).OnDelete(DeleteBehavior.Restrict);
    }
}
