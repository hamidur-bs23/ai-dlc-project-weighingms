using System.Net.Http.Json;
using ErrorOr;
using WeighingMS.Application.Devices;

namespace WeighingMS.Infrastructure.Integrations.Health;

/// <summary>Status-strip probe for the printer (status only; the printing adapter arrives in bolt 012).</summary>
public sealed class PrinterStatusProbe(HttpClient http) : IDeviceHealthProbe
{
    /// <inheritdoc />
    public string Device => "printer";

    /// <inheritdoc />
    public DeviceKind Kind => DeviceKind.Printer;

    /// <inheritdoc />
    public async Task<DeviceStatus> CheckAsync(CancellationToken cancellationToken)
    {
        var response = await IntegrationCall.SendAsync(
            ct => http.GetAsync(new Uri("api/printer/status", UriKind.Relative), ct), Error.Custom(503, "Printer.Unavailable", "Printer offline."), null, cancellationToken);
        if (response.IsError)
        {
            return DeviceStatus.Offline;
        }

        using var message = response.Value;
        var status = await message.Content.ReadFromJsonAsync<SimStatus>(IntegrationCall.Json, cancellationToken);
        return string.Equals(status?.Status, "READY", StringComparison.OrdinalIgnoreCase) ? DeviceStatus.Ready : DeviceStatus.Offline;
    }

    private sealed record SimStatus(string Status);
}
