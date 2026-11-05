namespace WeighingMS.Domain.Weighing;

/// <summary>Where a component's tolerance came from (decision TOL); kept on the record.</summary>
public enum ToleranceSource
{
    /// <summary>The recipe gave the tolerance.</summary>
    Recipe,

    /// <summary>The configured tolerance for chemical + scale class.</summary>
    Configured,
}
