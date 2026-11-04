using ErrorOr;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace WeighingMS.Api.Common;

/// <summary>
/// Base controller that maps ErrorOr errors to RFC 9457 ProblemDetails (api-conventions → Error Format):
/// Validation 422, Unauthorized 401, Forbidden 403, NotFound 404, Conflict 409, custom status codes as given.
/// <c>type</c> and <c>errorCode</c> carry the stable error code; <c>traceId</c> is always present.
/// </summary>
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>Converts a list of errors into a ProblemDetails result.</summary>
    protected IActionResult Problem(IReadOnlyList<Error> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        if (errors.Count > 0 && errors.All(e => e.Type == ErrorType.Validation))
        {
            var modelState = new ModelStateDictionary();
            foreach (var error in errors)
            {
                var field = error.Metadata?.TryGetValue("field", out var f) == true ? f.ToString() ?? string.Empty : string.Empty;
                modelState.AddModelError(field, error.Description);
            }

            var validation = ProblemDetailsFactory.CreateValidationProblemDetails(
                HttpContext, modelState, StatusCodes.Status422UnprocessableEntity, "Request is invalid", errors[0].Code);
            validation.Extensions["errorCode"] = errors[0].Code;
            return UnprocessableEntity(validation);
        }

        var first = errors[0];
        var status = first.Type switch
        {
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Validation => StatusCodes.Status422UnprocessableEntity,
            ErrorType.Unexpected or ErrorType.Failure => StatusCodes.Status500InternalServerError,
            _ => first.NumericType,
        };
        var problem = ProblemDetailsFactory.CreateProblemDetails(HttpContext, status, first.Description, first.Code, first.Description);
        problem.Extensions["errorCode"] = first.Code;
        return new ObjectResult(problem) { StatusCode = status, ContentTypes = { "application/problem+json" } };
    }
}
