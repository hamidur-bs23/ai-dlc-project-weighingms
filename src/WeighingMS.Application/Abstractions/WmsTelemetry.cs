using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace WeighingMS.Application.Abstractions;

/// <summary>
/// WeighingMS telemetry (coding-standards → Observability): one <see cref="ActivitySource"/> and one
/// <see cref="Meter"/> named <c>WeighingMS</c>. Spans are <c>wms.&lt;area&gt;.&lt;action&gt;</c>; metric labels are
/// low-cardinality only (never batch, operator, lot or container).
/// </summary>
public static class WmsTelemetry
{
    /// <summary>Source and meter name.</summary>
    public const string Name = "WeighingMS";

    /// <summary>Business spans.</summary>
    public static readonly ActivitySource Source = new(Name);

    /// <summary>Business metrics.</summary>
    public static readonly Meter Meter = new(Name);

    /// <summary>Duration of scale reads (healthy polls are measured here instead of traced).</summary>
    public static readonly Histogram<double> ScaleReadDuration =
        Meter.CreateHistogram<double>("wms.scale.read.duration", unit: "s", description: "Duration of a scale weight read");
}
