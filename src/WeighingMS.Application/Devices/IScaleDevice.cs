using ErrorOr;

namespace WeighingMS.Application.Devices;

/// <summary>Port to the station scales (boundary B1). Adapters translate transport failures into <see cref="DeviceErrors"/>.</summary>
public interface IScaleDevice
{
    /// <summary>All scales.</summary>
    Task<ErrorOr<IReadOnlyList<ScaleInfo>>> ListAsync(CancellationToken cancellationToken);

    /// <summary>Availability of one scale.</summary>
    Task<ErrorOr<DeviceStatus>> GetStatusAsync(string scaleId, CancellationToken cancellationToken);

    /// <summary>The current net reading.</summary>
    Task<ErrorOr<RawScaleReading>> ReadAsync(string scaleId, CancellationToken cancellationToken);

    /// <summary>Tares the scale.</summary>
    Task<ErrorOr<TareResult>> TareAsync(string scaleId, CancellationToken cancellationToken);
}
