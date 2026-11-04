using WeighingMS.Domain.Access;

namespace WeighingMS.Application.Access;

/// <summary>Hashes and verifies supervisor PINs. PINs are never stored or logged in clear text.</summary>
public interface IPinHasher
{
    /// <summary>Hashes a PIN for a user.</summary>
    string Hash(string pin);

    /// <summary>True when the PIN matches the user's stored hash.</summary>
    bool Verify(StationUser user, string pin);
}
