using ErrorOr;

namespace WeighingMS.Application.Devices;

/// <summary>Device and integration errors (codes in <c>Area.Reason</c> form).</summary>
public static class DeviceErrors
{
    /// <summary>The scale is offline, timing out or failing (503).</summary>
    public static Error ScaleUnavailable(string scaleId) =>
        Error.Custom(503, "Scale.Unavailable", $"Scale '{scaleId}' is offline.", new Dictionary<string, object> { ["scaleId"] = scaleId });

    /// <summary>No such scale (404).</summary>
    public static Error ScaleNotFound(string scaleId) =>
        Error.NotFound("Scale.NotFound", $"Scale '{scaleId}' does not exist.", new Dictionary<string, object> { ["scaleId"] = scaleId });

    /// <summary>The scanner is offline, timing out or failing (503).</summary>
    public static readonly Error ScannerUnavailable = Error.Custom(503, "Scanner.Unavailable", "The scanner is offline.");

    /// <summary>A cursor below zero (422).</summary>
    public static readonly Error InvalidCursor = Error.Validation("Request.Invalid", "The scan cursor must be zero or positive.");
}
