using WeighingMS.Domain.Common;
using WeighingMS.Domain.Tickets;

namespace WeighingMS.Domain.Tests;

/// <summary>The ticket's layout, state and print keys (bolt 012, FR-21, PD-02, PD-04).</summary>
public sealed class TicketTests
{
    private static readonly DateTimeOffset _now = new(2026, 11, 6, 4, 2, 41, TimeSpan.Zero);
    private static readonly DateTimeOffset _plantTime = new(2026, 11, 6, 10, 2, 41, TimeSpan.FromHours(6));

    private static TicketComponent Component(string name, decimal target, decimal actual, MassUnit unit = MassUnit.Gram, bool isOverride = false) =>
        new(name, Quantity.Of(target, unit), Quantity.Of(actual, unit), isOverride);

    private static TicketContent Content(IReadOnlyList<string>? operators = null, params TicketComponent[] components) =>
        new("B-24187", "SO-9931", "JET-04", "Teal 4C", _plantTime, operators ?? ["R. Karim"], components);

    private static Ticket NewTicket() => Ticket.Create("B-24187", ["line 1", "line 2"], _now);

    [Fact]
    public void Compose_LaysOutEveryFieldInOrder()
    {
        var lines = TicketComposer.Compose(Content(
            null,
            Component("Remazol Yellow RGB", 94.0m, 94.2m),
            Component("Sodium Sulphate", 24.000m, 23.980m, MassUnit.Kilogram)));

        lines.ShouldBe(
        [
            "       FCI DYEING - WEIGHING TICKET",
            "BATCH    B-24187",
            "ORDER    SO-9931",
            "MACHINE  JET-04",
            "SHADE    Teal 4C",
            "DATE     2026-11-06 10:02",
            "OPER.    R. Karim",
            "------------------------------------------",
            "COMPONENT                TGT           ACT",
            "Remazol Yellow RGB",
            "                      94.0 g        94.2 g",
            "Sodium Sulphate",
            "                   24.000 kg     23.980 kg",
            "------------------------------------------",
            "           ALL WITHIN TOLERANCE",
        ]);
    }

    [Fact]
    public void Compose_DateIsThePlantTimeGiven_NotUtc()
    {
        var lines = TicketComposer.Compose(Content(null, Component("X", 1m, 1m)));

        lines.ShouldContain("DATE     2026-11-06 10:02");
    }

    [Fact]
    public void Compose_AnOverride_IsMarkedAndCounted()
    {
        var lines = TicketComposer.Compose(Content(
            null,
            Component("Remazol Red RB", 386.0m, 386.0m),
            Component("Remazol Yellow RGB", 94.0m, 96.4m, isOverride: true),
            Component("Soda Ash", 9.600m, 9.900m, MassUnit.Kilogram, isOverride: true)));

        lines.Count(l => l.StartsWith("OVR", StringComparison.Ordinal)).ShouldBe(2);
        lines.ShouldContain("OVR                   94.0 g        96.4 g");
        lines[^1].Trim().ShouldBe("OVERRIDES: 2");
        lines.ShouldNotContain(l => l.Contains("ALL WITHIN", StringComparison.Ordinal));
    }

    [Fact]
    public void Compose_ListsEveryOperator_WrappingUnderTheLabel()
    {
        var lines = TicketComposer.Compose(Content(["R. Karim", "S. Akter", "A very long operator display name"], Component("X", 1m, 1m)));

        lines.ShouldContain("OPER.    R. Karim, S. Akter, A very long");
        lines.ShouldContain("         operator display name");
    }

    [Fact]
    public void Compose_ALongChemicalName_IsWrappedNeverCut()
    {
        const string Name = "Remazol Brilliant Blue RSPL Extra Concentrated Liquid Grade 150";

        var lines = TicketComposer.Compose(Content(null, Component(Name, 1m, 1m)));

        lines.ShouldAllBe(l => l.Length <= TicketComposer.Width);
        string.Join(' ', lines.SkipWhile(l => !l.StartsWith("COMPONENT", StringComparison.Ordinal)).Skip(1).TakeWhile(l => l.Trim().Length > 0 && !l.StartsWith(' '))).ShouldBe(Name);
    }

    [Fact]
    public void Compose_AnUnbrokenLongWord_IsSplitAndNothingIsLost()
    {
        var word = new string('A', TicketComposer.Width + 5);

        var lines = TicketComposer.Compose(Content(null, Component(word, 1m, 1m)));

        lines.ShouldAllBe(l => l.Length <= TicketComposer.Width);
        string.Concat(lines.Where(l => l.Trim('A').Length == 0)).ShouldBe(word);
    }

    [Theory]
    [InlineData(1240.0, MassUnit.Gram, "1240.0 g")]
    [InlineData(94.2, MassUnit.Gram, "94.2 g")]
    [InlineData(24, MassUnit.Kilogram, "24.000 kg")]
    public void Compose_ShowsEachUnitWithItsPrecision(double value, MassUnit unit, string expected)
    {
        var lines = TicketComposer.Compose(Content(null, Component("X", (decimal)value, (decimal)value, unit)));

        lines.ShouldContain(l => l.EndsWith(expected, StringComparison.Ordinal));
    }

    [Fact]
    public void Compose_DifferentUnits_ShowEachOwnUnit()
    {
        var lines = TicketComposer.Compose(Content(null, new TicketComponent("X", Quantity.Of(1m, MassUnit.Kilogram), Quantity.Of(1000.0m, MassUnit.Gram), false)));

        lines.ShouldContain(l => l.Contains("1.000 kg", StringComparison.Ordinal) && l.EndsWith("1000.0 g", StringComparison.Ordinal));
    }

    [Fact]
    public void Create_IsPrintPendingWithTheBatchIdAsBarcode()
    {
        var ticket = NewTicket();

        ticket.Status.ShouldBe(TicketStatus.PrintPending);
        ticket.PrintCount.ShouldBe(0);
        ticket.BarcodeValue.ShouldBe("B-24187");
        Ticket.BarcodeSymbology.ShouldBe("Code128");
        ticket.FirstPrintedAt.ShouldBeNull();
    }

    [Fact]
    public void Create_WithoutLines_Throws() =>
        Should.Throw<ArgumentException>(() => Ticket.Create("B-24187", [], _now));

    [Fact]
    public void FirstPrint_IsPlainAndUsesTheFixedKey()
    {
        var ticket = NewTicket();

        ticket.NextPrintIsReprint.ShouldBeFalse();
        ticket.NextPrintLines.ShouldBe(["line 1", "line 2"]);
        ticket.NextPrintKey.ShouldBe(ticket.FirstPrintKey);
    }

    [Fact]
    public void RecordPrinted_MakesTheNextPrintAReprintWithTheMarkFirst()
    {
        var ticket = NewTicket();

        ticket.RecordPrinted(_now);

        ticket.Status.ShouldBe(TicketStatus.Printed);
        ticket.PrintCount.ShouldBe(1);
        ticket.FirstPrintedAt.ShouldBe(_now);
        ticket.NextPrintIsReprint.ShouldBeTrue();
        ticket.NextPrintLines[0].Trim().ShouldBe(TicketComposer.ReprintMark);
        ticket.NextPrintLines.Skip(1).ShouldBe(["line 1", "line 2"]);
        ticket.Lines.ShouldBe(["line 1", "line 2"]); // the frozen ticket itself never carries the mark
    }

    [Fact]
    public void RecordPrinted_Twice_KeepsTheFirstTimeAndMovesTheLast()
    {
        var ticket = NewTicket();
        ticket.RecordPrinted(_now);

        ticket.RecordPrinted(_now.AddHours(1));

        ticket.FirstPrintedAt.ShouldBe(_now);
        ticket.LastPrintedAt.ShouldBe(_now.AddHours(1));
    }

    [Fact]
    public void NextPrintKey_IsStableUntilPrinted_AndDiffersPerCopy()
    {
        var ticket = NewTicket();
        var keys = new List<Guid>();

        for (var copy = 0; copy < 4; copy++)
        {
            ticket.NextPrintKey.ShouldBe(ticket.NextPrintKey);
            keys.Add(ticket.NextPrintKey);
            ticket.RecordPrinted(_now);
        }

        keys.Distinct().Count().ShouldBe(4);
    }

    [Fact]
    public void NextPrintKey_DiffersBetweenTickets()
    {
        var first = NewTicket();
        var second = NewTicket();
        first.RecordPrinted(_now);
        second.RecordPrinted(_now);

        first.NextPrintKey.ShouldNotBe(second.NextPrintKey);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    public void CheckPress_AcceptsOnlyTheCurrentPrintCount(int expected, bool accepted)
    {
        var ticket = NewTicket();
        ticket.RecordPrinted(_now);

        var press = ticket.CheckPress(expected);

        press.IsError.ShouldBe(!accepted);
        if (!accepted)
        {
            press.FirstError.Code.ShouldBe("Ticket.AlreadyPrinted");
        }
    }

    [Fact]
    public void PrintJob_RecordsWhetherItWasAReprintAndItsOutcome()
    {
        var ticket = NewTicket();

        var failed = PrintJob.Record(ticket, ticket.NextPrintKey, "OP-1001", "Printer.Unavailable", _now);
        ticket.RecordPrinted(_now);
        var reprint = PrintJob.Record(ticket, ticket.NextPrintKey, "OP-1002", null, _now);

        (failed.IsReprint, failed.ErrorCode, failed.OperatorBadgeId).ShouldBe((false, "Printer.Unavailable", "OP-1001"));
        (reprint.IsReprint, reprint.ErrorCode, reprint.TicketId).ShouldBe((true, null, ticket.Id));
    }
}
