using System.Globalization;
using System.Text;
using WeighingMS.Domain.Common;

namespace WeighingMS.Domain.Tickets;

/// <summary>One chemical on the ticket.</summary>
public sealed record TicketComponent(string Name, Quantity Target, Quantity Actual, bool IsOverride);

/// <summary>Everything a ticket shows. <paramref name="CompletedAt"/> is already in plant time (PD-06).</summary>
public sealed record TicketContent(
    string BatchId,
    string OrderId,
    string Machine,
    string Shade,
    DateTimeOffset CompletedAt,
    IReadOnlyList<string> Operators,
    IReadOnlyList<TicketComponent> Components);

/// <summary>
/// The fixed V1 layout (PD-04, FR-21) as text lines for 80 mm paper. Chemical names are never shortened (TICKET-LINES)
/// and every operator is listed (TICKET-OPERATORS); long values wrap instead of being cut.
/// </summary>
public static class TicketComposer
{
    /// <summary>Columns on 80 mm paper. The real printer is unconfirmed (Q18, A-04).</summary>
    public const int Width = 42;

    /// <summary>The first line of every print after the first (PD-02).</summary>
    public const string ReprintMark = "*** REPRINT ***";

    private const string _title = "FCI DYEING - WEIGHING TICKET";
    private const int _labelWidth = 9;
    private const int _markWidth = 14;
    private const int _columnWidth = 14;

    /// <summary>The ticket's lines, top to bottom. The barcode is sent separately, below them.</summary>
    public static IReadOnlyList<string> Compose(TicketContent content)
    {
        ArgumentNullException.ThrowIfNull(content);
        var rule = new string('-', Width);
        var overrides = content.Components.Count(c => c.IsOverride);
        var lines = new List<string> { Center(_title) };

        lines.AddRange(Field("BATCH", content.BatchId));
        lines.AddRange(Field("ORDER", content.OrderId));
        lines.AddRange(Field("MACHINE", content.Machine));
        lines.AddRange(Field("SHADE", content.Shade));
        lines.AddRange(Field("DATE", content.CompletedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)));
        lines.AddRange(Field("OPER.", string.Join(", ", content.Operators)));

        lines.Add(rule);
        lines.Add(Columns("COMPONENT", "TGT", "ACT"));
        foreach (var component in content.Components)
        {
            lines.AddRange(Wrap(component.Name, Width));
            lines.Add(Columns(component.IsOverride ? "OVR" : string.Empty, Format(component.Target), Format(component.Actual)));
        }

        lines.Add(rule);
        lines.Add(Center(overrides == 0 ? "ALL WITHIN TOLERANCE" : $"OVERRIDES: {overrides}"));
        return lines;
    }

    /// <summary>A reprint's lines: the same ticket with the REPRINT mark first.</summary>
    public static IReadOnlyList<string> MarkReprint(IReadOnlyList<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        return [Center(ReprintMark), .. lines];
    }

    private static IEnumerable<string> Field(string label, string value)
    {
        var wrapped = Wrap(value, Width - _labelWidth);
        for (var i = 0; i < wrapped.Count; i++)
        {
            yield return ((i == 0 ? label : string.Empty).PadRight(_labelWidth) + wrapped[i]).TrimEnd();
        }
    }

    private static string Columns(string mark, string target, string actual) =>
        (mark.PadRight(_markWidth) + target.PadLeft(_columnWidth) + actual.PadLeft(_columnWidth)).TrimEnd();

    private static string Format(Quantity quantity) =>
        string.Create(CultureInfo.InvariantCulture, $"{quantity.Value.ToString(quantity.Unit == MassUnit.Gram ? "0.0" : "0.000", CultureInfo.InvariantCulture)} {quantity.Symbol}");

    private static string Center(string text) => text.PadLeft((Width + text.Length) / 2);

    /// <summary>Breaks at spaces; a single word longer than the width is split, so nothing is ever lost.</summary>
    private static List<string> Wrap(string text, int width)
    {
        var lines = new List<string>();
        var current = new StringBuilder();
        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var rest = word;
            if (current.Length > 0 && current.Length + 1 + rest.Length > width)
            {
                lines.Add(current.ToString());
                current.Clear();
            }

            while (rest.Length > width)
            {
                lines.Add(rest[..width]);
                rest = rest[width..];
            }

            if (current.Length > 0)
            {
                current.Append(' ');
            }

            current.Append(rest);
        }

        lines.Add(current.ToString());
        return lines;
    }
}
