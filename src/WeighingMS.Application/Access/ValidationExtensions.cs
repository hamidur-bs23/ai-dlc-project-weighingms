using ErrorOr;
using FluentValidation.Results;

namespace WeighingMS.Application.Access;

/// <summary>Turns FluentValidation results into ErrorOr validation errors.</summary>
internal static class ValidationExtensions
{
    public static List<Error> ToErrors(this ValidationResult result) =>
        [.. result.Errors.Select(f => Error.Validation(f.ErrorCode is { Length: > 0 } c && c.Contains('.', StringComparison.Ordinal) ? c : "Request.Invalid", f.ErrorMessage, new Dictionary<string, object> { ["field"] = f.PropertyName }))];
}
