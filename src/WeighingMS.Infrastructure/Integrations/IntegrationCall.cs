using System.Net;
using System.Text.Json;
using ErrorOr;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace WeighingMS.Infrastructure.Integrations;

/// <summary>
/// Runs one HTTP call to an external party and translates transport failures (connection errors, timeouts,
/// an open circuit, 5xx) into the given unavailable error, so <see cref="HttpRequestException"/> never leaks upward.
/// A 404 becomes the given not-found error.
/// </summary>
internal static class IntegrationCall
{
    internal static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static async Task<ErrorOr<HttpResponseMessage>> SendAsync(
        Func<CancellationToken, Task<HttpResponseMessage>> send,
        Error unavailable,
        Error? notFound,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await send(cancellationToken);
        }
        catch (HttpRequestException)
        {
            return unavailable;
        }
        catch (TimeoutRejectedException)
        {
            return unavailable;
        }
        catch (BrokenCircuitException)
        {
            return unavailable;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return unavailable;
        }

        if (response.StatusCode == HttpStatusCode.NotFound && notFound is { } missing)
        {
            response.Dispose();
            return missing;
        }

        if (!response.IsSuccessStatusCode)
        {
            response.Dispose();
            return unavailable;
        }

        return response;
    }
}
