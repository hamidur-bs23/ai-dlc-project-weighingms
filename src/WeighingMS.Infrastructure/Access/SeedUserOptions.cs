namespace WeighingMS.Infrastructure.Access;

/// <summary>
/// A lab user created by the development seeder (section <c>StationAccess:SeedUsers</c>, Development and Test
/// only). Dev seed data, not real credentials.
/// </summary>
public sealed class SeedUserOptions
{
    /// <summary>Badge number.</summary>
    public string BadgeId { get; set; } = string.Empty;

    /// <summary>Display name.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary><c>Operator</c> or <c>Supervisor</c>.</summary>
    public string Role { get; set; } = "Operator";

    /// <summary>Supervisor PIN (hashed when seeded).</summary>
    public string? Pin { get; set; }
}
