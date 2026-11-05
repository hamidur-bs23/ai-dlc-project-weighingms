using ErrorOr;

namespace WeighingMS.Domain.Weighing;

/// <summary>Free-text identifiers captured for an attempt (A6): trimmed, 1–32 characters, no recipe check in V1.</summary>
public static class AttemptText
{
    /// <summary>The longest lot or container ID accepted.</summary>
    public const int MaxLength = 32;

    /// <summary>A trimmed, non-empty value of at most <paramref name="maxLength"/> characters, or a field error.</summary>
    public static ErrorOr<string> Create(string? value, string field, int maxLength = MaxLength)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        if (trimmed.Length == 0 || trimmed.Length > maxLength)
        {
            return Error.Validation("Request.Invalid", $"{field} must be 1–{maxLength} characters.", new Dictionary<string, object> { ["field"] = field });
        }

        return trimmed;
    }
}
