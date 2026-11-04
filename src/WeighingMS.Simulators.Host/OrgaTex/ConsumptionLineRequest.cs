using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WeighingMS.Simulators.Host.OrgaTex;

/// <summary>Actual consumption of one chemical (training contract).</summary>
public sealed record ConsumptionLineRequest(
    [Required(AllowEmptyStrings = false)] string ChemicalId,
    [property: JsonRequired][Range(typeof(decimal), "0", "1000000")] decimal ActualQuantity,
    [Required(AllowEmptyStrings = false), RegularExpression("^(g|kg)$")] string Unit,
    [Required(AllowEmptyStrings = false)] string Lot);
