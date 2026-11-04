using System.ComponentModel.DataAnnotations;

namespace WeighingMS.Simulators.Host.Scanner;

/// <summary>Simulation control: one barcode read by the dummy scanner.</summary>
public sealed record ScanRequest([Required(AllowEmptyStrings = false), MaxLength(128)] string Code);
