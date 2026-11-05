using ErrorOr;

namespace WeighingMS.Domain.Weighing;

/// <summary>Weighing-session errors (codes in <c>Area.Reason</c> form).</summary>
public static class WeighingErrors
{
    /// <summary>No session has been started for the batch (404).</summary>
    public static Error SessionNotFound(string batchId) =>
        Error.NotFound("Weighing.SessionNotFound", $"No weighing session exists for batch '{batchId}'.", new Dictionary<string, object> { ["batchId"] = batchId });

    /// <summary>The batch has no component with this sequence (404).</summary>
    public static Error ComponentNotFound(int sequence) =>
        Error.NotFound("Weighing.ComponentNotFound", $"The batch has no component {sequence}.", new Dictionary<string, object> { ["sequence"] = sequence });

    /// <summary>A confirmed component cannot be selected again (409, FR-4).</summary>
    public static Error ComponentDone(int sequence) =>
        Error.Conflict("Weighing.ComponentDone", $"Component {sequence} is already weighed.", new Dictionary<string, object> { ["sequence"] = sequence });

    /// <summary>No component is being weighed (409).</summary>
    public static readonly Error NoActiveComponent = Error.Conflict("Weighing.NoActiveComponent", "Select a component first.");

    /// <summary>A scale the configuration assigns is not in the station's scale list (503: configuration/device mismatch).</summary>
    public static Error ScaleNotAvailable(string scaleId) =>
        Error.Custom(503, "Weighing.ScaleNotAvailable", $"The assigned scale '{scaleId}' is not available at this station.", new Dictionary<string, object> { ["scaleId"] = scaleId });

    /// <summary>Another change to the same session was saved first (409, assumption A2).</summary>
    public static readonly Error ConcurrencyConflict = Error.Conflict("Concurrency.Conflict", "This batch was changed elsewhere. Reload and try again.");

    /// <summary>The component was confirmed already: a repeated confirm (double tap) changes nothing (409, A8).</summary>
    public static Error AlreadyConfirmed(int sequence) =>
        Error.Conflict("Weighing.AlreadyConfirmed", $"Component {sequence} is already confirmed.", new Dictionary<string, object> { ["sequence"] = sequence });

    /// <summary>The command names a component that is not the active one (409).</summary>
    public static Error ComponentNotActive(int sequence) =>
        Error.Conflict("Weighing.ComponentNotActive", $"Component {sequence} is not being weighed. Select it first.", new Dictionary<string, object> { ["sequence"] = sequence });

    /// <summary>
    /// The confirm rule is not met (422): one validation error per missing condition (field = the condition), so the
    /// response lists them. The error code is <c>Weighing.OutOfTolerance</c> when tolerance is the only gap.
    /// </summary>
    public static List<Error> NotConfirmable(ConfirmationCheck check)
    {
        ArgumentNullException.ThrowIfNull(check);
        var onlyTolerance = check.Missing.Count == 1 && check.Missing[0] == ConfirmationGap.InTolerance;
        return [.. check.Missing.Select(gap => Error.Validation(
            onlyTolerance ? "Weighing.OutOfTolerance" : "Weighing.NotConfirmable",
            Describe(gap, check.Classification),
            new Dictionary<string, object> { ["field"] = GapName(gap) }))];
    }

    /// <summary>The wire name of a gap (camelCase), as listed in <c>missing</c>.</summary>
    public static string GapName(ConfirmationGap gap)
    {
        var name = gap.ToString();
        return string.Concat(char.ToLowerInvariant(name[0]).ToString(), name[1..]);
    }

    private static string Describe(ConfirmationGap gap, Classification? classification) => gap switch
    {
        ConfirmationGap.Tolerance => "No tolerance is defined for this chemical.",
        ConfirmationGap.Lot => "Enter or scan the lot.",
        ConfirmationGap.Container => "Enter the container.",
        ConfirmationGap.Tare => "Tare the empty container on this scale.",
        ConfirmationGap.TareStale => "The scale was offline since the tare: tare again.",
        ConfirmationGap.Reading => "The scale reading can't be used.",
        ConfirmationGap.Stable => "Wait for a stable reading.",
        _ => classification is { } c ? $"The weight is {c.Class}: not within tolerance." : "The weight is not within tolerance.",
    };

    /// <summary>The reading is in tolerance: confirm normally instead of overriding (409, story 011 edge case).</summary>
    public static Error OverrideNotNeeded(int sequence) =>
        Error.Conflict("Weighing.OverrideNotNeeded", $"Component {sequence} is within tolerance: confirm it normally.", new Dictionary<string, object> { ["sequence"] = sequence });

    /// <summary>The batch is completed; the session can only be read (409, A17).</summary>
    public static Error BatchCompleted(string batchId) =>
        Error.Conflict("Weighing.BatchCompleted", $"Batch '{batchId}' is completed and can't be changed.", new Dictionary<string, object> { ["batchId"] = batchId });

    /// <summary>Completion while components are still pending (409, story 013).</summary>
    public static Error Incomplete(string batchId, int done, int total) =>
        Error.Conflict("Batch.Incomplete", $"Batch '{batchId}' has {done} of {total} components weighed.", new Dictionary<string, object> { ["batchId"] = batchId, ["done"] = done, ["total"] = total });
}
