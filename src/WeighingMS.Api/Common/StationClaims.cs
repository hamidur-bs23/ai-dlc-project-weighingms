using System.Security.Claims;

namespace WeighingMS.Api.Common;

/// <summary>Claim types carried by the station session cookie.</summary>
public static class StationClaims
{
    /// <summary>Badge number.</summary>
    public const string Badge = "wms:badge";

    /// <summary>Shift, if given at sign-in.</summary>
    public const string Shift = "wms:shift";

    /// <summary>Sign-in time (UTC, ISO-8601).</summary>
    public const string SignedInAt = "wms:signedInAt";

    /// <summary>The signed-in badge, or null.</summary>
    public static string? BadgeOf(ClaimsPrincipal user) => user?.FindFirstValue(Badge);
}
