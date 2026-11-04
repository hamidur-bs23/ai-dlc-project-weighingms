using WeighingMS.Application.Abstractions;
using WeighingMS.Application.Access;

namespace WeighingMS.Infrastructure.Access;

/// <summary>
/// <see cref="IPlantClock"/> over <see cref="TimeProvider"/> and the configured IANA zone. An unknown zone
/// throws at construction, so start-up fails instead of showing wrong times.
/// </summary>
public sealed class PlantClock : IPlantClock
{
    private readonly TimeProvider _time;
    private readonly TimeZoneInfo _zone;

    /// <summary>Creates the clock for the configured zone.</summary>
    /// <exception cref="InvalidOperationException">The zone is not a known IANA time zone.</exception>
    public PlantClock(TimeProvider time, StationAccessOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _time = time;
        try
        {
            _zone = TimeZoneInfo.FindSystemTimeZoneById(options.TimeZone);
        }
        catch (TimeZoneNotFoundException ex)
        {
            throw new InvalidOperationException($"Station:TimeZone '{options.TimeZone}' is not a known IANA time zone.", ex);
        }

        TimeZoneId = options.TimeZone;
    }

    /// <inheritdoc />
    public string TimeZoneId { get; }

    /// <inheritdoc />
    public DateTimeOffset UtcNow => _time.GetUtcNow();

    /// <inheritdoc />
    public DateTimeOffset ToPlantTime(DateTimeOffset instant) => TimeZoneInfo.ConvertTime(instant, _zone);
}
