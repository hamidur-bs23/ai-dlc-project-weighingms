namespace WeighingMS.Simulators.Host.Faults;

/// <summary>Names of the dummies that can be faulted. A scale target is <c>scale:{scaleId}</c>.</summary>
public static class FaultTargets
{
    /// <summary>The dummy OrgaTEX.</summary>
    public const string OrgaTex = "orgatex";

    /// <summary>The dummy printer.</summary>
    public const string Printer = "printer";

    /// <summary>The dummy scanner.</summary>
    public const string Scanner = "scanner";

    /// <summary>Prefix of a scale target.</summary>
    public const string ScalePrefix = "scale:";

    /// <summary>Marker used by the fault filter: resolve the scale from the <c>scaleId</c> route value.</summary>
    public const string ScaleFromRoute = "scale";

    /// <summary>The target name of one scale.</summary>
    public static string Scale(string scaleId) => ScalePrefix + scaleId.ToLowerInvariant();

    /// <summary>Normalises a target name (trimmed, lower case).</summary>
    public static string Normalise(string target) => target.Trim().ToLowerInvariant();

    /// <summary>The <c>Area.Unavailable</c> error code for a target.</summary>
    public static string UnavailableCode(string target) => target switch
    {
        OrgaTex => "OrgaTex.Unavailable",
        Printer => "Printer.Unavailable",
        Scanner => "Scanner.Unavailable",
        _ => "Scale.Unavailable",
    };
}
