namespace WeighingMS.Domain.Access;

/// <summary>Role of a station user (ADR-009).</summary>
public enum StationRole
{
    /// <summary>Weighs components; signs in with a badge.</summary>
    Operator,

    /// <summary>Authorises privileged actions with badge + PIN (step-up).</summary>
    Supervisor,
}
