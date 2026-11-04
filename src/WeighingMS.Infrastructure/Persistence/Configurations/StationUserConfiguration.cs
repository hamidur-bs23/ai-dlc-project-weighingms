using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeighingMS.Domain.Access;

namespace WeighingMS.Infrastructure.Persistence.Configurations;

/// <summary>Table <c>station_users</c>.</summary>
public sealed class StationUserConfiguration : IEntityTypeConfiguration<StationUser>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<StationUser> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("station_users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();
        builder.Property(u => u.BadgeId).HasMaxLength(32).IsRequired();
        builder.HasIndex(u => u.BadgeId).IsUnique();
        builder.Property(u => u.DisplayName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.Role).HasConversion<string>().HasMaxLength(16);
        builder.Property(u => u.PinHash).HasMaxLength(256);
        builder.Property<uint>("xmin").IsRowVersion();
    }
}
