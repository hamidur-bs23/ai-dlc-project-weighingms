namespace WeighingMS.Simulators.Host.Printer;

/// <summary>A barcode to print (for example Code 128 of the batch ID).</summary>
public sealed record BarcodeDto(string Symbology, string Value);
