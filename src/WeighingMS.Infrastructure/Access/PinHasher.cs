using Microsoft.AspNetCore.Identity;
using WeighingMS.Application.Access;
using WeighingMS.Domain.Access;

namespace WeighingMS.Infrastructure.Access;

/// <summary>PIN hashing with ASP.NET Core Identity's <see cref="PasswordHasher{TUser}"/> (PBKDF2).</summary>
public sealed class PinHasher : IPinHasher
{
    private readonly PasswordHasher<StationUser> _hasher = new();

    /// <inheritdoc />
    public string Hash(string pin) => _hasher.HashPassword(null!, pin);

    /// <inheritdoc />
    public bool Verify(StationUser user, string pin)
    {
        ArgumentNullException.ThrowIfNull(user);
        return user.PinHash is { } hash && _hasher.VerifyHashedPassword(user, hash, pin) != PasswordVerificationResult.Failed;
    }
}
