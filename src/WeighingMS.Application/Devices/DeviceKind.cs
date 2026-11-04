namespace WeighingMS.Application.Devices;

/// <summary>Kinds of station devices and integrations shown in the status strip.</summary>
public enum DeviceKind
{
    /// <summary>A weighing scale.</summary>
    Scale,

    /// <summary>The barcode scanner.</summary>
    Scanner,

    /// <summary>The ticket printer.</summary>
    Printer,

    /// <summary>The OrgaTEX MES (wire name <c>orgatex</c>).</summary>
    Orgatex,
}
