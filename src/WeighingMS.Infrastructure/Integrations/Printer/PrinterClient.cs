using System.Net.Http.Json;
using ErrorOr;
using WeighingMS.Application.Tickets;

namespace WeighingMS.Infrastructure.Integrations.Printer;

/// <summary>
/// Typed client to the dummy printer: <c>POST /api/tickets</c> with an <c>Idempotency-Key</c> (training contract, not a
/// claim about a real printer). Any failure means the ticket wasn't printed as far as we know, so it maps to
/// <see cref="PrinterErrors.Unavailable"/>; a repeated key makes pressing again safe.
/// </summary>
public sealed class PrinterClient(HttpClient http) : IPrinter
{
    /// <inheritdoc />
    public async Task<ErrorOr<DateTimeOffset>> PrintAsync(TicketPrint print, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(print);
        var response = await IntegrationCall.SendAsync(
            async ct =>
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("api/tickets", UriKind.Relative))
                {
                    Content = JsonContent.Create(
                        new SimTicket(print.BatchId, print.IsReprint, print.Lines, new SimBarcode(print.BarcodeSymbology, print.BarcodeValue)),
                        options: IntegrationCall.Json),
                };
                request.Headers.Add("Idempotency-Key", print.IdempotencyKey.ToString());
                return await http.SendAsync(request, ct);
            },
            PrinterErrors.Unavailable,
            null,
            cancellationToken);
        if (response.IsError)
        {
            return response.Errors;
        }

        using var message = response.Value;
        var receipt = await message.Content.ReadFromJsonAsync<SimReceipt>(IntegrationCall.Json, cancellationToken);
        return receipt?.PrintedAt ?? DateTimeOffset.UtcNow;
    }

    private sealed record SimTicket(string BatchId, bool IsReprint, IReadOnlyList<string> Lines, SimBarcode Barcode);

    private sealed record SimBarcode(string Symbology, string Value);

    private sealed record SimReceipt(DateTimeOffset PrintedAt);
}
