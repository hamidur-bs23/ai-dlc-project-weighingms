using Microsoft.AspNetCore.Mvc;

namespace WeighingMS.Simulators.Host.Common;

/// <summary>
/// Builds RFC 9457 ProblemDetails responses whose <c>type</c> is an <c>Area.Reason</c> error code
/// (coding-standards → Error Handling).
/// </summary>
public static class SimProblems
{
    /// <summary>Creates a ProblemDetails result with the given status, error code and detail.</summary>
    public static ObjectResult Create(int status, string code, string title, string? detail = null)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Type = code,
            Title = title,
            Detail = detail,
        };
        return new ObjectResult(problem)
        {
            StatusCode = status,
            ContentTypes = { "application/problem+json" },
        };
    }

    /// <summary>404 for an unknown batch.</summary>
    public static ObjectResult BatchNotFound(string batchId) =>
        Create(StatusCodes.Status404NotFound, "Batch.NotFound", "Batch not found", $"Batch '{batchId}' is not in the OrgaTEX queue.");

    /// <summary>404 for an unknown scale.</summary>
    public static ObjectResult ScaleNotFound(string scaleId) =>
        Create(StatusCodes.Status404NotFound, "Scale.NotFound", "Scale not found", $"Scale '{scaleId}' does not exist.");
}
