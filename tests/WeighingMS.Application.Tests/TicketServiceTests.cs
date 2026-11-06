using ErrorOr;
using Microsoft.Extensions.Time.Testing;
using WeighingMS.Application.Tickets;
using WeighingMS.Domain.Audit;
using WeighingMS.Domain.Tickets;

namespace WeighingMS.Application.Tests;

/// <summary>Reading and printing a ticket with fakes (bolt 012).</summary>
public sealed class TicketServiceTests
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 11, 6, 4, 2, 41, TimeSpan.Zero));
    private readonly FakeTickets _tickets = new();
    private readonly FakePrinter _printer = new();
    private readonly FakeAudit _audit = new();
    private readonly TicketService _service;

    public TicketServiceTests() => _service = new TicketService(_tickets, _printer, _audit, new FakePlantClock(_time));

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private Ticket Stored()
    {
        var ticket = Ticket.Create("B-24187", ["line 1"], _time.GetUtcNow());
        _tickets.Add(ticket);
        return ticket;
    }

    [Fact]
    public async Task Get_WithoutATicket_IsNotFound()
    {
        var result = await _service.GetAsync("B-24187", Ct);

        result.FirstError.Code.ShouldBe("Ticket.NotFound");
        result.FirstError.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public async Task Get_ShowsTheSameContentThatPrints()
    {
        Stored();

        var view = (await _service.GetAsync("B-24187", Ct)).Value;

        view.Lines.ShouldBe(["line 1"]);
        (view.BarcodeSymbology, view.BarcodeValue).ShouldBe(("Code128", "B-24187"));
        view.Status.ShouldBe(TicketStatus.PrintPending);
    }

    [Fact]
    public async Task Print_First_IsPlain_RecordedAndAudited()
    {
        var ticket = Stored();
        var key = ticket.FirstPrintKey;

        var view = (await _service.PrintAsync("B-24187", 0, "OP-1001", Ct)).Value;

        var sent = _printer.Prints.ShouldHaveSingleItem();
        (sent.IsReprint, sent.IdempotencyKey, sent.BarcodeValue).ShouldBe((false, key, "B-24187"));
        sent.Lines.ShouldBe(["line 1"]);
        (view.Status, view.PrintCount).ShouldBe((TicketStatus.Printed, 1));
        var job = _tickets.Jobs.ShouldHaveSingleItem();
        (job.IsReprint, job.ErrorCode, job.OperatorBadgeId, job.IdempotencyKey).ShouldBe((false, null, "OP-1001", key));
        var entry = _audit.Entries.ShouldHaveSingleItem();
        (entry.Type, entry.ActorBadgeId, entry.Subject).ShouldBe((AuditEventType.TicketPrinted, "OP-1001", "B-24187"));
    }

    [Fact]
    public async Task Print_Second_IsMarkedReprintWithANewKeyAndItsOwnAudit()
    {
        var ticket = Stored();
        await _service.PrintAsync("B-24187", 0, "OP-1001", Ct);

        await _service.PrintAsync("B-24187", 1, "OP-1002", Ct);

        var sent = _printer.Prints[1];
        sent.IsReprint.ShouldBeTrue();
        sent.Lines[0].Trim().ShouldBe(TicketComposer.ReprintMark);
        sent.IdempotencyKey.ShouldNotBe(ticket.FirstPrintKey);
        _tickets.Jobs[1].IsReprint.ShouldBeTrue();
        var entry = _audit.Entries[1];
        (entry.Type, entry.ActorBadgeId, entry.Reason).ShouldBe((AuditEventType.TicketReprinted, "OP-1002", "print 2"));
    }

    [Fact]
    public async Task Print_AStalePress_IsRefusedWithoutTouchingThePrinter()
    {
        Stored();
        await _service.PrintAsync("B-24187", 0, "OP-1001", Ct);

        var second = await _service.PrintAsync("B-24187", 0, "OP-1001", Ct);

        second.FirstError.Code.ShouldBe("Ticket.AlreadyPrinted");
        _printer.Prints.Count.ShouldBe(1);
        _audit.Entries.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Print_WithoutATicket_IsNotFound()
    {
        (await _service.PrintAsync("B-24187", 0, "OP-1001", Ct)).FirstError.Code.ShouldBe("Ticket.NotFound");

        _printer.Prints.ShouldBeEmpty();
    }

    [Fact]
    public async Task Print_PrinterDown_LeavesItPendingAndRecordsTheFailedAttemptWithoutAudit()
    {
        var ticket = Stored();
        _printer.Down = true;

        var result = await _service.PrintAsync("B-24187", 0, "OP-1001", Ct);

        result.FirstError.Code.ShouldBe("Printer.Unavailable");
        (ticket.Status, ticket.PrintCount).ShouldBe((TicketStatus.PrintPending, 0));
        _tickets.Jobs.ShouldHaveSingleItem().ErrorCode.ShouldBe("Printer.Unavailable");
        _tickets.Saves.ShouldBe(1);
        _audit.Entries.ShouldBeEmpty();
    }

    [Fact]
    public async Task Print_AfterRecovery_UsesTheSameKey_SoALostResponseCantPrintTwice()
    {
        Stored();
        _printer.Down = true;
        await _service.PrintAsync("B-24187", 0, "OP-1001", Ct);
        var keyTried = _tickets.Jobs[0].IdempotencyKey;
        _printer.Down = false;

        var result = await _service.PrintAsync("B-24187", 0, "OP-1001", Ct);

        result.Value.PrintCount.ShouldBe(1);
        _printer.Prints.ShouldHaveSingleItem().IdempotencyKey.ShouldBe(keyTried);
    }

    [Fact]
    public async Task Print_LosingARaceToAnotherPress_IsAConflict()
    {
        Stored();
        _tickets.Conflict = true;

        var result = await _service.PrintAsync("B-24187", 0, "OP-1001", Ct);

        result.FirstError.Code.ShouldBe("Ticket.AlreadyPrinted");
    }

    private sealed class FakePrinter : IPrinter
    {
        public bool Down { get; set; }

        public List<TicketPrint> Prints { get; } = [];

        public Task<ErrorOr<DateTimeOffset>> PrintAsync(TicketPrint print, CancellationToken cancellationToken)
        {
            if (Down)
            {
                return Task.FromResult<ErrorOr<DateTimeOffset>>(PrinterErrors.Unavailable);
            }

            Prints.Add(print);
            return Task.FromResult<ErrorOr<DateTimeOffset>>(DateTimeOffset.UnixEpoch);
        }
    }
}
