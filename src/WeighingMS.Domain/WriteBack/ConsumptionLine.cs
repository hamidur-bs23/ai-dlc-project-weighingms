namespace WeighingMS.Domain.WriteBack;

/// <summary>One chemical's actual consumption as posted to OrgaTEX (FR-20): chemical, quantity and unit, and lot.</summary>
public sealed record ConsumptionLine(string ChemicalId, decimal ActualQuantity, string Unit, string Lot);
