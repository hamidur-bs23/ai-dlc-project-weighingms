using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WeighingMS.Application.Weighing;
using WeighingMS.Application.WriteBack;
using WeighingMS.Domain.Audit;
using WeighingMS.Domain.Tickets;
using WeighingMS.Domain.Weighing;

namespace WeighingMS.Api.IntegrationTests;

/// <summary>Bolt 012 through the Api and the real dummy printer: STEP 27 and the printer-unavailable part of STEP 30.</summary>
public sealed class TicketEndpointsTests(ApiFixture fixture) : IAsyncLifetime
{
    private const string _batch = "B-24187";
    private static readonly TimeSpan _pour = TimeSpan.FromSeconds(8);
    private static readonly (int Seq, decimal Target, string Unit)[] _teal4C =
        [(1, 1240.0m, "g"), (2, 386.0m, "g"), (3, 94.0m, "g"), (4, 24.000m, "kg"), (5, 9.600m, "kg")];

    private readonly SimulatorHost _simulator = new();
    private WebApplicationFactory<Program> _api = null!;
    private StationClient _client = null!;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        await fixture.ResetAsync();
        await HostAsync();
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _api.DisposeAsync();
        await _simulator.DisposeAsync();
    }

    private async Task HostAsync(string badge = "OP-1001", Action<IServiceCollection>? services = null)
    {
        _api = fixture.CreateApi(simulator: _simulator, services: services);
        _client = new StationClient(_api);
        (await _client.SignInAsync(badge)).EnsureSuccessStatusCode();
        (await Session("")).IsSuccessStatusCode.ShouldBeTrue();
    }

    private async Task RehostAsync(string badge = "OP-1001", Action<IServiceCollection>? services = null)
    {
        _client.Dispose();
        await _api.DisposeAsync();
        await HostAsync(badge, services);
    }

    private Task<HttpResponseMessage> Session(string path, object? body = null) =>
        _client.SendAsync(HttpMethod.Post, $"/api/batches/{_batch}/session{path}", body);

    private Task<HttpResponseMessage> Print(int expectedPrintCount) => Session("/ticket/print", new { expectedPrintCount });

    private static async Task<JsonElement> Expect(Task<HttpResponseMessage> call, HttpStatusCode status)
    {
        var response = await call;
        response.StatusCode.ShouldBe(status, await response.Content.ReadAsStringAsync(Ct));
        return await response.ReadJsonAsync();
    }

    private async Task PourAsync(int sequence, string scenario, decimal target, string unit, decimal? tolerance = null)
    {
        var scaleId = unit == "g" ? "scale-3kg" : "scale-32kg";
        await _simulator.PostAsync("/_sim/reset", new { });
        await Expect(Session("/selection", new { sequence }), HttpStatusCode.OK);
        await Expect(Session("/attempt/lot", new { sequence, lot = $"L-{sequence}" }), HttpStatusCode.OK);
        await Expect(Session("/attempt/container", new { sequence, container = $"C-{sequence}" }), HttpStatusCode.OK);
        await Expect(Session("/tare", new { sequence }), HttpStatusCode.OK);
        await _simulator.PostAsync($"/_sim/scales/{scaleId}/scenario", new { scenario, target = new { value = target, unit }, tolerance });
        _simulator.Clock.Advance(_pour);
    }

    private async Task WeighAsync(params int[] sequences)
    {
        foreach (var (seq, target, unit) in _teal4C.Where(c => sequences.Contains(c.Seq)))
        {
            await PourAsync(seq, "rising", target, unit);
            await Expect(Session("/confirmation", new { sequence = seq }), HttpStatusCode.Created);
        }
    }

    private Task WeighAllAsync() => WeighAsync(1, 2, 3, 4, 5);

    private async Task<JsonElement> PrintsAsync()
    {
        var response = await _simulator.Control.GetAsync(new Uri("/_sim/printer/tickets", UriKind.Relative), Ct);
        return await response.EnsureSuccessStatusCode().ReadJsonAsync();
    }

    private async Task<JsonElement> TicketAsync() =>
        await Expect(_client.GetAsync($"/api/batches/{_batch}/session/ticket"), HttpStatusCode.OK);

    private async Task<Ticket> RowAsync()
    {
        await using var db = fixture.NewDbContext();
        return await db.Tickets.SingleAsync(Ct);
    }

    private async Task<string?> QueueStatusAsync()
    {
        var queue = await Expect(_client.GetAsync("/api/batches"), HttpStatusCode.OK);
        return queue.GetProperty("items").EnumerateArray().Single(b => b.GetProperty("batchId").GetString() == _batch).GetProperty("status").GetString();
    }

    private async Task<JsonElement> SummaryTicketAsync() =>
        (await Expect(_client.GetAsync($"/api/batches/{_batch}/session/summary"), HttpStatusCode.OK)).GetProperty("ticket");

    private static List<string?> Lines(JsonElement e) => [.. e.GetProperty("lines").EnumerateArray().Select(l => l.GetString())];

    // ── Story 001: compose ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Completion_ComposesTheTicketWithEveryStep27Field_AndPrintsNothing()
    {
        await WeighAllAsync();

        var ticket = await TicketAsync();

        ticket.GetProperty("state").GetString().ShouldBe("printPending");
        ticket.GetProperty("printCount").GetInt32().ShouldBe(0);
        Lines(ticket).ShouldBe(
        [
            "       FCI DYEING - WEIGHING TICKET",
            "BATCH    B-24187",
            "ORDER    SO-9931",
            "MACHINE  JET-04",
            "SHADE    Teal 4C",
            "DATE     2026-11-04 03:48", // the fixture's 21:48 UTC in plant time (Asia/Dhaka)
            "OPER.    R. Karim",
            "------------------------------------------",
            "COMPONENT                TGT           ACT",
            "Remazol Brill. Blue RSPL",
            "                    1240.0 g      1240.0 g",
            "Remazol Red RB",
            "                     386.0 g       386.0 g",
            "Remazol Yellow RGB",
            "                      94.0 g        94.0 g",
            "Sodium Sulphate",
            "                   24.000 kg     24.000 kg",
            "Soda Ash",
            "                    9.600 kg      9.600 kg",
            "------------------------------------------",
            "           ALL WITHIN TOLERANCE",
        ]);
        ticket.GetProperty("barcode").GetProperty("symbology").GetString().ShouldBe("Code128");
        ticket.GetProperty("barcode").GetProperty("value").GetString().ShouldBe(_batch);
        (await PrintsAsync()).GetArrayLength().ShouldBe(0); // TICKET-PRINT-MANUAL
    }

    [Fact]
    public async Task Completion_WithAnOverride_MarksTheLineAndCountsIt()
    {
        await WeighAsync(1, 2, 4, 5);
        await PourAsync(3, "overshoot", 94.0m, "g", 0.5m);
        var grant = await Expect(_client.SendAsync(HttpMethod.Post, "/api/session/step-up", new { badgeId = "SV-2001", pin = "2468", action = "override" }), HttpStatusCode.OK);

        await Expect(Session("/override", new { sequence = 3, grantId = grant.GetProperty("grantId").GetGuid(), reason = "Shade lab accepts" }), HttpStatusCode.Created);

        var lines = Lines(await TicketAsync());
        lines.ShouldContain("OVR                   94.0 g        96.4 g");
        lines[^1]!.Trim().ShouldBe("OVERRIDES: 1");
    }

    [Fact]
    public async Task Completion_ByTwoOperators_ListsBothInTheOrderTheyFirstWeighed()
    {
        await WeighAsync(1);
        fixture.Clock.Advance(TimeSpan.FromMinutes(1));
        await Expect(Session("/leave"), HttpStatusCode.OK);
        await RehostAsync("OP-1002");

        await WeighAsync(2, 3, 4, 5);

        Lines(await TicketAsync()).ShouldContain("OPER.    R. Karim, S. Akter");
    }

    [Fact]
    public async Task ReviewList_ByTwoOperators_ListsBothInTheOrderTheyFirstWeighed()
    {
        await WeighAsync(1);
        fixture.Clock.Advance(TimeSpan.FromMinutes(1));
        await Expect(Session("/leave"), HttpStatusCode.OK);
        await RehostAsync("OP-1002");

        await WeighAsync(2, 3, 4, 5);

        var list = await Expect(_client.GetAsync("/api/weighing-sessions?status=completed"), HttpStatusCode.OK);
        list.GetProperty("items")[0].GetProperty("operators").EnumerateArray().Select(o => o.GetString()).ShouldBe(["R. Karim", "S. Akter"]);
    }

    [Fact]
    public async Task Completion_AFailingHandler_LeavesNoTicketAndNoCompletion()
    {
        await RehostAsync(services: s => s.AddScoped<IBatchCompletedHandler, FailingHandler>());
        await WeighAsync(1, 2, 3, 4);
        await PourAsync(5, "rising", 9.600m, "kg");

        (await Session("/confirmation", new { sequence = 5 })).StatusCode.ShouldBe(HttpStatusCode.InternalServerError);

        await using var db = fixture.NewDbContext();
        (await db.Tickets.CountAsync(Ct)).ShouldBe(0);
        (await db.WeighingSessions.SingleAsync(Ct)).Status.ShouldBe(WeighingSessionStatus.InProgress);
    }

    [Fact]
    public async Task Ticket_BeforeCompletion_Is404AndTheSummarySaysNotAvailable()
    {
        var response = await _client.GetAsync($"/api/batches/{_batch}/session/ticket");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.ErrorCodeAsync()).ShouldBe("Ticket.NotFound");
        (await SummaryTicketAsync()).GetProperty("state").GetString().ShouldBe("notAvailable");
    }

    [Fact]
    public async Task Ticket_SurvivesAnApiRestart()
    {
        await WeighAllAsync();
        var before = Lines(await TicketAsync());

        await RehostAsync();

        Lines(await TicketAsync()).ShouldBe(before);
    }

    [Fact]
    public async Task Database_RefusesASecondTicketForTheSameBatch()
    {
        await using var db = fixture.NewDbContext();
        db.Tickets.Add(Ticket.Create(_batch, ["x"], fixture.Clock.GetUtcNow()));
        db.Tickets.Add(Ticket.Create(_batch, ["y"], fixture.Clock.GetUtcNow()));

        await Should.ThrowAsync<DbUpdateException>(() => db.SaveChangesAsync(Ct));
    }

    // ── Story 002: print ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Step27_Print_SendsTheSameContentOnce_AndEveryScreenFieldFollows()
    {
        await WeighAllAsync();
        var composed = await TicketAsync();

        var printed = await Expect(Print(0), HttpStatusCode.OK);

        printed.GetProperty("state").GetString().ShouldBe("printed");
        printed.GetProperty("printCount").GetInt32().ShouldBe(1);
        printed.GetProperty("firstPrintedAt").ValueKind.ShouldBe(JsonValueKind.String);
        var prints = await PrintsAsync();
        prints.GetArrayLength().ShouldBe(1);
        prints[0].GetProperty("batchId").GetString().ShouldBe(_batch);
        prints[0].GetProperty("isReprint").GetBoolean().ShouldBeFalse();
        prints[0].GetProperty("idempotencyKey").GetString().ShouldBe((await RowAsync()).FirstPrintKey.ToString());
        prints[0].GetProperty("barcode").GetProperty("symbology").GetString().ShouldBe("Code128");
        prints[0].GetProperty("barcode").GetProperty("value").GetString().ShouldBe(_batch);
        Lines(prints[0]).ShouldBe(Lines(composed));
        var summary = await SummaryTicketAsync();
        (summary.GetProperty("state").GetString(), summary.GetProperty("printCount").GetInt32()).ShouldBe(("printed", 1));
        await using var db = fixture.NewDbContext();
        var audit = await db.AuditEntries.SingleAsync(a => a.Type == AuditEventType.TicketPrinted, Ct);
        (audit.ActorBadgeId, audit.Subject).ShouldBe(("OP-1001", _batch));
        (await db.PrintJobs.SingleAsync(Ct)).ErrorCode.ShouldBeNull();
    }

    [Fact]
    public async Task Print_ADoublePress_PrintsOnce()
    {
        await WeighAllAsync();
        await Expect(Print(0), HttpStatusCode.OK);

        var second = await Print(0);

        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await second.ErrorCodeAsync()).ShouldBe("Ticket.AlreadyPrinted");
        (await PrintsAsync()).GetArrayLength().ShouldBe(1);
        (await TicketAsync()).GetProperty("printCount").GetInt32().ShouldBe(1);
    }

    [Theory]
    [InlineData(0)] // first print
    [InlineData(1)] // reprint
    public async Task Print_SixConcurrentPresses_PrintOneCopy(int alreadyPrinted)
    {
        await WeighAllAsync();
        for (var i = 0; i < alreadyPrinted; i++)
        {
            await Expect(Print(i), HttpStatusCode.OK);
        }

        var responses = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => Print(alreadyPrinted)));

        var outcomes = string.Join(" | ", await Task.WhenAll(responses.Select(async r => $"{(int)r.StatusCode} {await r.Content.ReadAsStringAsync(Ct)}")));
        responses.Count(r => r.StatusCode == HttpStatusCode.OK).ShouldBe(1, outcomes);
        responses.Count(r => r.StatusCode == HttpStatusCode.Conflict).ShouldBe(5, outcomes);
        (await PrintsAsync()).GetArrayLength().ShouldBe(alreadyPrinted + 1);
        (await TicketAsync()).GetProperty("printCount").GetInt32().ShouldBe(alreadyPrinted + 1);
    }

    [Fact]
    public async Task Print_WithoutTheExpectedCount_Is422AndPrintsNothing()
    {
        await WeighAllAsync();

        (await Session("/ticket/print", new { })).StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);

        (await PrintsAsync()).GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public async Task Print_BeforeCompletion_Is404()
    {
        var response = await Print(0);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.ErrorCodeAsync()).ShouldBe("Ticket.NotFound");
    }

    [Fact]
    public async Task Print_NotSignedIn_Is401()
    {
        await using var api = fixture.CreateApi(simulator: _simulator);
        using var anonymous = new StationClient(api);
        await anonymous.PrimeAntiforgeryAsync();

        (await anonymous.SendAsync(HttpMethod.Post, $"/api/batches/{_batch}/session/ticket/print", new { expectedPrintCount = 0 })).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── Story 003: reprint ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Reprint_IsMarked_NewKey_Audited_AndTheFrozenTicketIsUnchanged()
    {
        await WeighAllAsync();
        var composed = Lines(await TicketAsync());
        await Expect(Print(0), HttpStatusCode.OK);

        var again = await Expect(Print(1), HttpStatusCode.OK);

        again.GetProperty("printCount").GetInt32().ShouldBe(2);
        var prints = await PrintsAsync();
        prints.GetArrayLength().ShouldBe(2);
        var original = prints.EnumerateArray().Single(p => !p.GetProperty("isReprint").GetBoolean());
        var reprint = prints.EnumerateArray().Single(p => p.GetProperty("isReprint").GetBoolean());
        reprint.GetProperty("idempotencyKey").GetString().ShouldNotBe(original.GetProperty("idempotencyKey").GetString());
        Lines(original).ShouldBe(composed);
        var reprinted = Lines(reprint);
        reprinted[0]!.Trim().ShouldBe("*** REPRINT ***");
        reprinted.Skip(1).ShouldBe(composed);
        Lines(await TicketAsync()).ShouldBe(composed);
        await using var db = fixture.NewDbContext();
        (await db.AuditEntries.SingleAsync(a => a.Type == AuditEventType.TicketReprinted, Ct)).Reason.ShouldBe("print 2");
    }

    [Fact]
    public async Task Reprint_ByAnotherOperator_IsAllowedAndAuditedAgainstThem()
    {
        await WeighAllAsync();
        await Expect(Print(0), HttpStatusCode.OK);
        await using var api = fixture.CreateApi(simulator: _simulator);
        using var other = new StationClient(api);
        (await other.SignInAsync("OP-1002")).EnsureSuccessStatusCode();

        var response = await other.SendAsync(HttpMethod.Post, $"/api/batches/{_batch}/session/ticket/print", new { expectedPrintCount = 1 });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        await using var db = fixture.NewDbContext();
        (await db.AuditEntries.SingleAsync(a => a.Type == AuditEventType.TicketReprinted, Ct)).ActorBadgeId.ShouldBe("OP-1002");
    }

    // ── Story 004: printer unavailable ──────────────────────────────────────────────────

    [Fact]
    public async Task Step30_PrinterDownAtCompletion_BatchCompletes_TicketPending_StripOffline_NoAutoPrintAfterRecovery()
    {
        await WeighAsync(1, 2, 3, 4);
        await PourAsync(5, "rising", 9.600m, "kg");
        await _simulator.FaultAsync("printer", "unavailable");

        await Expect(Session("/confirmation", new { sequence = 5 }), HttpStatusCode.Created);

        await using (var db = fixture.NewDbContext())
        {
            (await db.WeighingSessions.SingleAsync(Ct)).Status.ShouldBe(WeighingSessionStatus.Completed);
        }

        (await SummaryTicketAsync()).GetProperty("state").GetString().ShouldBe("printPending");
        var health = await Expect(_client.GetAsync("/api/devices/health"), HttpStatusCode.OK);
        health.GetProperty("items").EnumerateArray().Single(d => d.GetProperty("device").GetString() == "printer").GetProperty("status").GetString().ShouldBe("offline");

        var refused = await Print(0);
        refused.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        (await refused.ErrorCodeAsync()).ShouldBe("Printer.Unavailable");
        (await TicketAsync()).GetProperty("state").GetString().ShouldBe("printPending");

        await _simulator.ClearFaultsAsync();
        (await TicketAsync()).GetProperty("printCount").GetInt32().ShouldBe(0); // no auto-print on recovery
        (await PrintsAsync()).GetArrayLength().ShouldBe(0);

        await Expect(Print(0), HttpStatusCode.OK);

        var prints = await PrintsAsync();
        prints.GetArrayLength().ShouldBe(1);
        prints[0].GetProperty("isReprint").GetBoolean().ShouldBeFalse();
        await using var after = fixture.NewDbContext();
        var jobs = await after.PrintJobs.OrderBy(j => j.AttemptedAt).ThenBy(j => j.ErrorCode == null).ToListAsync(Ct);
        jobs.Select(j => j.ErrorCode).ShouldBe(["Printer.Unavailable", null]);
        jobs.Select(j => j.IdempotencyKey).Distinct().Count().ShouldBe(1);
    }

    [Theory]
    [InlineData("error500")]
    [InlineData("timeout")]
    public async Task Print_OtherPrinterFaults_AreUnavailableAndLeaveItPending(string mode)
    {
        await WeighAllAsync();
        await _simulator.FaultAsync("printer", mode);

        (await Print(0)).StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);

        (await RowAsync()).Status.ShouldBe(TicketStatus.PrintPending);
    }

    [Fact]
    public async Task Reprint_PrinterDown_KeepsTheCountAndPressingAgainPrintsOneCopy()
    {
        await WeighAllAsync();
        await Expect(Print(0), HttpStatusCode.OK);
        await _simulator.FaultAsync("printer", "unavailable");

        (await Print(1)).StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        (await RowAsync()).PrintCount.ShouldBe(1);
        await _simulator.ClearFaultsAsync();

        await Expect(Print(1), HttpStatusCode.OK);

        (await PrintsAsync()).GetArrayLength().ShouldBe(2);
    }

    // ── Queue status ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task QueueStatus_ShowsWhatStillNeedsAttention_ThenComplete()
    {
        await WeighAllAsync();
        (await QueueStatusAsync()).ShouldBe("syncPending"); // written to OrgaTEX comes first

        await using (var scope = _api.Services.CreateAsyncScope())
        {
            (await scope.ServiceProvider.GetRequiredService<WriteBackDispatcher>().DispatchDueAsync(Ct)).Sent.ShouldBe(1);
        }

        (await QueueStatusAsync()).ShouldBe("printPending");

        await Expect(Print(0), HttpStatusCode.OK);

        (await QueueStatusAsync()).ShouldBe("complete");
    }

    [Fact]
    public async Task QueueStatus_PrintedButNotSynced_StaysSyncPending()
    {
        await WeighAllAsync();

        await Expect(Print(0), HttpStatusCode.OK);

        (await QueueStatusAsync()).ShouldBe("syncPending");
    }

    private sealed class FailingHandler : IBatchCompletedHandler
    {
        public Task HandleAsync(BatchCompletedNotice notice, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("handler failed");
    }
}
