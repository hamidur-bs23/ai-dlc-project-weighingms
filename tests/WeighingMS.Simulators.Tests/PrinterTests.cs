using System.Net;
using System.Net.Http.Json;

namespace WeighingMS.Simulators.Tests;

public sealed class PrinterTests : IDisposable
{
    private readonly SimulatorFactory _factory = new();
    private readonly HttpClient _client;

    public PrinterTests() => _client = _factory.CreateClient();

    private static object Ticket(string batchId = "B-24187", bool isReprint = false) => new
    {
        batchId,
        isReprint,
        lines = new[] { "FCI DYEING - WEIGHING TICKET", "BATCH   B-24187" },
        barcode = new { symbology = "code128", value = batchId },
    };

    private Task<HttpResponseMessage> PrintAsync(string? key, object body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/tickets") { Content = JsonContent.Create(body) };
        if (key is not null)
        {
            request.Headers.Add("Idempotency-Key", key);
        }

        return _client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Print_ValidTicket_Returns201WithReceipt()
    {
        var response = await PrintAsync("p-1", Ticket());
        var json = await response.ReadJsonAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        json.GetProperty("batchId").GetString().ShouldBe("B-24187");
        json.GetProperty("isReprint").GetBoolean().ShouldBeFalse();
        json.GetProperty("barcode").GetProperty("value").GetString().ShouldBe("B-24187");
    }

    [Fact]
    public async Task Print_RepeatedKey_PrintsOnceAndReturnsOriginalReceipt()
    {
        var first = await (await PrintAsync("p-2", Ticket())).ReadJsonAsync();
        var second = await (await PrintAsync("p-2", Ticket())).ReadJsonAsync();
        var printed = await (await _client.GetUrlAsync("/_sim/printer/tickets")).ReadJsonAsync();

        second.GetProperty("printJobId").GetString().ShouldBe(first.GetProperty("printJobId").GetString());
        printed.GetArrayLength().ShouldBe(1);
    }

    [Fact]
    public async Task Print_MissingKey_Returns400()
    {
        var response = await PrintAsync(null, Ticket());

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ProblemTypeAsync()).ShouldBe("Printer.IdempotencyKeyMissing");
    }

    [Fact]
    public async Task Print_NoBatchOrNoLines_Returns400()
    {
        (await PrintAsync("p-3", new { isReprint = false, lines = new[] { "x" } })).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await PrintAsync("p-4", new { batchId = "B-24187", isReprint = false, lines = Array.Empty<string>() })).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Inspection_ListsEachTicketWithItsContent()
    {
        await PrintAsync("p-5", Ticket());
        await PrintAsync("p-6", Ticket(isReprint: true));

        var printed = await (await _client.GetUrlAsync("/_sim/printer/tickets")).ReadJsonAsync();

        printed.GetArrayLength().ShouldBe(2);
        printed.EnumerateArray().ShouldContain(t => t.GetProperty("isReprint").GetBoolean());
        printed[0].GetProperty("lines")[1].GetString().ShouldBe("BATCH   B-24187");
    }

    [Fact]
    public async Task Status_NoFault_IsReady()
    {
        (await (await _client.GetUrlAsync("/api/printer/status")).ReadJsonAsync()).GetProperty("status").GetString().ShouldBe("READY");
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
