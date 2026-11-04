using ErrorOr;

namespace WeighingMS.Application.Devices;

/// <summary>Port to the barcode scanner (device-style, PD-03; no keyboard wedge).</summary>
public interface IScanner
{
    /// <summary>Scans with a sequence greater than <paramref name="after"/>, in order.</summary>
    Task<ErrorOr<IReadOnlyList<ScanEvent>>> GetScansAfterAsync(long after, CancellationToken cancellationToken);

    /// <summary>Availability of the scanner.</summary>
    Task<ErrorOr<DeviceStatus>> GetStatusAsync(CancellationToken cancellationToken);
}
