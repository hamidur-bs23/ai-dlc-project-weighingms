using System.Net.Http.Json;
using System.Text.Json;
using ErrorOr;
using WeighingMS.Application.Devices;

namespace WeighingMS.Infrastructure.Integrations.Scales;

/// <summary>
/// <see cref="IScaleDevice"/> over the dummy Scale API (training contract, decision SIM-CONTRACTS): translates its
/// plain numbers into WeighingMS quantities and its failures into <see cref="DeviceErrors"/>.
/// </summary>
public sealed class ScaleDeviceClient(HttpClient http) : IScaleDevice
{
    /// <inheritdoc />
    public async Task<ErrorOr<IReadOnlyList<ScaleInfo>>> ListAsync(CancellationToken cancellationToken)
    {
        var response = await IntegrationCall.SendAsync(ct => http.GetAsync(new Uri("api/scales", UriKind.Relative), ct), DeviceErrors.ScaleUnavailable("*"), null, cancellationToken);
        if (response.IsError)
        {
            return response.Errors;
        }

        using var message = response.Value;
        var scales = await message.Content.ReadFromJsonAsync<List<SimScale>>(IntegrationCall.Json, cancellationToken) ?? [];
        return scales.Select(s => new ScaleInfo(s.ScaleId, s.Name, s.ScaleClass, new DeviceQuantity(s.Capacity, s.Unit), new DeviceQuantity(s.Resolution, s.Unit))).ToList();
    }

    /// <inheritdoc />
    public async Task<ErrorOr<DeviceStatus>> GetStatusAsync(string scaleId, CancellationToken cancellationToken)
    {
        var response = await IntegrationCall.SendAsync(
            ct => http.GetAsync(new Uri($"api/scales/{Uri.EscapeDataString(scaleId)}/status", UriKind.Relative), ct),
            DeviceErrors.ScaleUnavailable(scaleId), DeviceErrors.ScaleNotFound(scaleId), cancellationToken);
        if (response.IsError)
        {
            return response.Errors;
        }

        using var message = response.Value;
        var status = await message.Content.ReadFromJsonAsync<SimStatus>(IntegrationCall.Json, cancellationToken);
        return string.Equals(status?.Status, "READY", StringComparison.OrdinalIgnoreCase) ? DeviceStatus.Ready : DeviceStatus.Offline;
    }

    /// <inheritdoc />
    public async Task<ErrorOr<RawScaleReading>> ReadAsync(string scaleId, CancellationToken cancellationToken)
    {
        var response = await IntegrationCall.SendAsync(
            ct => http.GetAsync(new Uri($"api/scales/{Uri.EscapeDataString(scaleId)}/weight", UriKind.Relative), ct),
            DeviceErrors.ScaleUnavailable(scaleId), DeviceErrors.ScaleNotFound(scaleId), cancellationToken);
        if (response.IsError)
        {
            return response.Errors;
        }

        using var message = response.Value;
        SimReading? reading;
        try
        {
            reading = await message.Content.ReadFromJsonAsync<SimReading>(IntegrationCall.Json, cancellationToken);
        }
        catch (JsonException)
        {
            reading = null;
        }

        return reading is null
            ? new RawScaleReading(scaleId, null, null, false, false, DateTimeOffset.UtcNow)
            : new RawScaleReading(scaleId, reading.Net, reading.Unit, reading.Stable, reading.OverCapacity, reading.Timestamp);
    }

    /// <inheritdoc />
    public async Task<ErrorOr<TareResult>> TareAsync(string scaleId, CancellationToken cancellationToken)
    {
        var response = await IntegrationCall.SendAsync(
            ct => http.PostAsync(new Uri($"api/scales/{Uri.EscapeDataString(scaleId)}/tare", UriKind.Relative), null, ct),
            DeviceErrors.ScaleUnavailable(scaleId), DeviceErrors.ScaleNotFound(scaleId), cancellationToken);
        if (response.IsError)
        {
            return response.Errors;
        }

        using var message = response.Value;
        var tare = await message.Content.ReadFromJsonAsync<SimTare>(IntegrationCall.Json, cancellationToken);
        return tare is null
            ? DeviceErrors.ScaleUnavailable(scaleId)
            : new TareResult(tare.ScaleId, new DeviceQuantity(tare.Tare, tare.Unit), tare.Timestamp);
    }

    private sealed record SimScale(string ScaleId, string Name, string ScaleClass, string Unit, decimal Capacity, decimal Resolution);

    private sealed record SimStatus(string ScaleId, string Status);

    private sealed record SimReading(string ScaleId, decimal? Net, string? Unit, bool Stable, bool OverCapacity, DateTimeOffset Timestamp);

    private sealed record SimTare(string ScaleId, decimal Tare, string Unit, DateTimeOffset Timestamp);
}
