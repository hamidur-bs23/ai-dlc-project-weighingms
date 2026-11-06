namespace WeighingMS.Domain.WriteBack;

/// <summary>
/// The automatic retry schedule (WRITEBACK-RETRY): at most <see cref="MaxAttempts"/> attempts; the wait after attempt n
/// is <c>BaseDelay × 2^(n-1)</c>, capped at <see cref="MaxDelay"/>. Defaults 5 attempts, 5 s, 5 min (configuration).
/// </summary>
public sealed record RetryPolicy(int MaxAttempts, TimeSpan BaseDelay, TimeSpan MaxDelay)
{
    /// <summary>The wait after the given (1-based) failed attempt.</summary>
    public TimeSpan DelayAfter(int attempt)
    {
        var factor = Math.Pow(2, Math.Max(0, attempt - 1));
        var delay = TimeSpan.FromTicks((long)Math.Min(BaseDelay.Ticks * factor, MaxDelay.Ticks));
        return delay < MaxDelay ? delay : MaxDelay;
    }
}
