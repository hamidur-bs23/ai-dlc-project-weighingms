using System.Net.Http.Json;
using ErrorOr;
using WeighingMS.Application.Devices;

namespace WeighingMS.Infrastructure.Integrations.Scanner;

/// <summary><see cref="IScanner"/> over the dummy scanner's cursor contract (device-style, PD-03).</summary>
public sealed class ScannerClient(HttpClient http) : IScanner
{
    /// <inheritdoc />
    public async Task<ErrorOr<IReadOnlyList<ScanEvent>>> GetScansAfterAsync(long after, CancellationToken cancellationToken)
    {
        var response = await IntegrationCall.SendAsync(
            ct => http.GetAsync(new Uri($"api/scanner/scans?after={after}", UriKind.Relative), ct), DeviceErrors.ScannerUnavailable, null, cancellationToken);
        if (response.IsError)
        {
            return response.Errors;
        }

        using var message = response.Value;
        var scans = await message.Content.ReadFromJsonAsync<List<SimScan>>(IntegrationCall.Json, cancellationToken) ?? [];
        return scans.Select(s => new ScanEvent(s.Sequence, s.Code, s.ScannedAt)).ToList();
    }

    /// <inheritdoc />
    public async Task<ErrorOr<DeviceStatus>> GetStatusAsync(CancellationToken cancellationToken)
    {
        var response = await IntegrationCall.SendAsync(
            ct => http.GetAsync(new Uri("api/scanner/status", UriKind.Relative), ct), DeviceErrors.ScannerUnavailable, null, cancellationToken);
        if (response.IsError)
        {
            return response.Errors;
        }

        using var message = response.Value;
        var status = await message.Content.ReadFromJsonAsync<SimStatus>(IntegrationCall.Json, cancellationToken);
        return string.Equals(status?.Status, "READY", StringComparison.OrdinalIgnoreCase) ? DeviceStatus.Ready : DeviceStatus.Offline;
    }

    private sealed record SimScan(long Sequence, string Code, DateTimeOffset ScannedAt);

    private sealed record SimStatus(string Status);
}
