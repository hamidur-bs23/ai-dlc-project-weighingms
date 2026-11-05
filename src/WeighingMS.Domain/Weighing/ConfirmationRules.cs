namespace WeighingMS.Domain.Weighing;

/// <summary>
/// The confirm rule (FR-13), in one place for both the Confirm button and the server's re-check: tolerance resolved;
/// lot, container and a valid tare present; a valid, stable reading classed Within range or Target achieved.
/// </summary>
public static class ConfirmationRules
{
    /// <summary>Checks the component's attempt against a reading from its own scale.</summary>
    public static ConfirmationCheck Check(ComponentWeighing component, MeasuredReading reading)
    {
        ArgumentNullException.ThrowIfNull(component);
        ArgumentNullException.ThrowIfNull(reading);
        var missing = new List<ConfirmationGap>();
        var attempt = component.Attempt;

        if (component.Tolerance.IsBlocked)
        {
            missing.Add(ConfirmationGap.Tolerance);
        }

        if (string.IsNullOrEmpty(attempt?.Lot))
        {
            missing.Add(ConfirmationGap.Lot);
        }

        if (string.IsNullOrEmpty(attempt?.Container))
        {
            missing.Add(ConfirmationGap.Container);
        }

        if (attempt?.Tare is not { } tare || tare.ScaleId != component.ScaleId)
        {
            missing.Add(ConfirmationGap.Tare);
        }
        else if (reading.OnlineSince is { } since && tare.TakenAt < since)
        {
            missing.Add(ConfirmationGap.TareStale);
        }

        if (reading.Net is null || !reading.Valid)
        {
            missing.Add(ConfirmationGap.Reading);
        }

        if (!reading.Stable)
        {
            missing.Add(ConfirmationGap.Stable);
        }

        var classification = reading.Net is { } net ? component.Classify(net) : null;
        if (classification is not { IsConfirmable: true })
        {
            missing.Add(ConfirmationGap.InTolerance);
        }

        return new ConfirmationCheck(missing, classification);
    }
}
