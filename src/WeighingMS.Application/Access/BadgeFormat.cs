using System.Text.RegularExpressions;

namespace WeighingMS.Application.Access;

/// <summary>Badge number format: 1–32 letters, digits or dashes, checked after trimming surrounding spaces.</summary>
public static partial class BadgeFormat
{
    /// <summary>True when the trimmed badge is valid.</summary>
    public static bool IsValid(string? badgeId) =>
        badgeId is not null && Pattern().IsMatch(badgeId.Trim());

    [GeneratedRegex("^[A-Za-z0-9-]{1,32}$", RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();
}
