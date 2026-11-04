namespace WeighingMS.Application.Access;

/// <summary>Station and access configuration (section <c>Station</c>).</summary>
public sealed class StationAccessOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Station";

    /// <summary>Station identifier shown in the top bar, for example <c>STATION 01</c>.</summary>
    public string StationId { get; set; } = "STATION 01";

    /// <summary>Plant IANA time zone (PD-06).</summary>
    public string TimeZone { get; set; } = "Asia/Dhaka";

    /// <summary>How long a step-up grant stays valid.</summary>
    public TimeSpan StepUpGrantLifetime { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>Wrong PINs before a supervisor is locked out.</summary>
    public int MaxFailedPinAttempts { get; set; } = 5;

    /// <summary>How long a lockout lasts.</summary>
    public TimeSpan LockoutDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Actions a supervisor can authorise with a step-up grant.</summary>
    public IReadOnlyList<string> StepUpActions { get; set; } = ["override"];
}
