using ErrorOr;
using WeighingMS.Application.Devices;

namespace WeighingMS.Infrastructure.Integrations.Health;

/// <summary>
/// Status-strip probe for OrgaTEX: reachable when the batch queue answers. The full OrgaTEX adapter
/// (<c>IOrgatexGateway</c>) arrives in bolt 005.
/// </summary>
public sealed class OrgatexReachabilityProbe(HttpClient http) : IDeviceHealthProbe
{
    /// <inheritdoc />
    public string Device => "orgatex";

    /// <inheritdoc />
    public DeviceKind Kind => DeviceKind.Orgatex;

    /// <inheritdoc />
    public async Task<DeviceStatus> CheckAsync(CancellationToken cancellationToken)
    {
        var response = await IntegrationCall.SendAsync(
            ct => http.GetAsync(new Uri("api/batches", UriKind.Relative), HttpCompletionOption.ResponseHeadersRead, ct),
            Error.Custom(503, "OrgaTex.Unavailable", "OrgaTEX unreachable."), null, cancellationToken);
        if (response.IsError)
        {
            return DeviceStatus.Offline;
        }

        response.Value.Dispose();
        return DeviceStatus.Ready;
    }
}
