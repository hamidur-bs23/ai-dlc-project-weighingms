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
}
