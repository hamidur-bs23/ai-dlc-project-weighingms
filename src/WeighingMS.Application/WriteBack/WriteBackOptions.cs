using WeighingMS.Domain.WriteBack;

namespace WeighingMS.Application.WriteBack;

/// <summary>
/// OrgaTEX write-back settings (section <c>WriteBack</c>; WRITEBACK-RETRY): the retry schedule, how often the dispatcher
/// looks for due entries, and whether the background dispatcher runs (tests drive it directly).
/// </summary>
public sealed class WriteBackOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "WriteBack";

    /// <summary>Attempts before "sync failed".</summary>
    public int MaxAttempts { get; set; }

    /// <summary>The wait after the first failure; doubled after each further one.</summary>
    public TimeSpan BaseDelay { get; set; }

    /// <summary>The longest wait between attempts.</summary>
    public TimeSpan MaxDelay { get; set; }

    /// <summary>How often the dispatcher checks for due entries (it also wakes on every completion).</summary>
    public TimeSpan PollInterval { get; set; }

    /// <summary>Whether the background dispatcher runs.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>The domain retry policy.</summary>
    public RetryPolicy Policy => new(MaxAttempts, BaseDelay, MaxDelay);

    /// <summary>Validation errors (empty when valid).</summary>
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (MaxAttempts < 1)
        {
            errors.Add("MaxAttempts must be at least 1");
        }

        if (BaseDelay <= TimeSpan.Zero || MaxDelay < BaseDelay)
        {
            errors.Add("BaseDelay must be positive and MaxDelay at least BaseDelay");
        }

        if (PollInterval <= TimeSpan.Zero)
        {
            errors.Add("PollInterval must be positive");
        }

        return errors;
    }
}
