using ErrorOr;

namespace WeighingMS.Domain.Access;

/// <summary>Error catalogue for station access (codes in <c>Area.Reason</c> form).</summary>
public static class AccessErrors
{
    /// <summary>No signed-in operator.</summary>
    public static readonly Error SignInRequired = Error.Unauthorized("Auth.SignInRequired", "Sign in with your badge first.");

    /// <summary>The badge is not a known station user.</summary>
    public static readonly Error BadgeNotRecognised = Error.Unauthorized("Auth.BadgeNotRecognised", "Badge not recognised.");

    /// <summary>The badge belongs to someone who is not a supervisor.</summary>
    public static readonly Error SupervisorRequired = Error.Forbidden("Auth.SupervisorRequired", "A supervisor badge is required.");

    /// <summary>Wrong supervisor PIN (or unknown supervisor badge).</summary>
    public static readonly Error StepUpFailed = Error.Forbidden("Auth.StepUpFailed", "Supervisor authorisation failed.");

    /// <summary>The supervisor is locked out after too many wrong PINs.</summary>
    public static readonly Error LockedOut = Error.Custom(429, "Auth.LockedOut", "Too many wrong PINs; try again later.");

    /// <summary>Four eyes (OVR-FOUR-EYES): the authorising supervisor is the signed-in operator (403).</summary>
    public static readonly Error FourEyesRequired = Error.Forbidden("Auth.FourEyesRequired", "Another supervisor must authorise this: the signed-in operator cannot approve their own override.");

    /// <summary>A step-up grant cannot be used (unknown, already used, expired or for another action).</summary>
    public static Error GrantInvalid(string reason) => Error.Forbidden("Auth.GrantInvalid", $"Supervisor authorisation is not valid: {reason}.");
}
