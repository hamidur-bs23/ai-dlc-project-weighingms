using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WeighingMS.Simulators.Host.Printer;

/// <summary>
/// A ticket to print (training contract). The printer prints the given text lines and barcode as they are;
/// layout and content are decided by WeighingMS (bolt 012).
/// </summary>
public sealed record TicketRequest(
    [Required(AllowEmptyStrings = false)] string BatchId,
    [property: JsonRequired] bool IsReprint,
    [Required, MinLength(1)] IReadOnlyList<string> Lines,
    BarcodeDto? Barcode);
