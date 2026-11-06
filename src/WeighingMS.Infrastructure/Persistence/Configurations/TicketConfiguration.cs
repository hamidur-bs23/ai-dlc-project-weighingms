using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeighingMS.Domain.Tickets;

namespace WeighingMS.Infrastructure.Persistence.Configurations;

/// <summary>
/// Table <c>tickets</c>: one per batch, the frozen lines as <c>jsonb</c>, <c>xmin</c> concurrency between presses.
/// </summary>
public sealed class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("tickets");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.Property(t => t.BatchId).HasMaxLength(32).IsRequired();
        builder.HasIndex(t => t.BatchId).IsUnique();
        builder.Property(t => t.BarcodeValue).HasMaxLength(32).IsRequired();
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property<uint>("xmin").IsRowVersion();
        builder.Ignore(t => t.Lines);
        builder.Ignore(t => t.NextPrintIsReprint);
        builder.Ignore(t => t.NextPrintLines);
        builder.Ignore(t => t.NextPrintKey);
        builder.Property<List<string>>("_lines")
            .HasColumnName("lines")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, _json),
                v => JsonSerializer.Deserialize<List<string>>(v, _json) ?? new List<string>(),
                new ValueComparer<List<string>>(
                    (a, b) => (a ?? new List<string>()).SequenceEqual(b ?? new List<string>()),
                    v => v.Aggregate(0, (h, l) => HashCode.Combine(h, l.GetHashCode(StringComparison.Ordinal))),
                    v => v.ToList()));
    }
}
