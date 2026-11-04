namespace WeighingMS.Application.Abstractions;

/// <summary>
/// The plant clock: the current UTC time, and conversion to the configured plant time zone (PD-06).
/// The server's own time zone never affects the result.
/// </summary>
public interface IPlantClock
{
    /// <summary>The configured IANA time zone, for example <c>Asia/Dhaka</c>.</summary>
    string TimeZoneId { get; }

    /// <summary>Now, in UTC.</summary>
    DateTimeOffset UtcNow { get; }

    /// <summary>Converts a UTC instant to plant local time (with the plant's offset).</summary>
    DateTimeOffset ToPlantTime(DateTimeOffset instant);
}
