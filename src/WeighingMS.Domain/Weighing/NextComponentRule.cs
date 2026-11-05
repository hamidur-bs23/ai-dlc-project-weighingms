namespace WeighingMS.Domain.Weighing;

/// <summary>
/// Decision NEXT-COMPONENT: after a confirm, the next pending component after the confirmed one, in recipe sequence,
/// wrapping to the start; none when nothing is pending.
/// </summary>
public static class NextComponentRule
{
    /// <summary>The sequence to activate next, or null.</summary>
    public static int? After(IEnumerable<ComponentWeighing> components, int confirmedSequence)
    {
        var pending = components.Where(c => c.State == ComponentState.Pending).Select(c => c.Sequence).Order().ToList();
        if (pending.Count == 0)
        {
            return null;
        }

        return pending.FirstOrDefault(s => s > confirmedSequence, pending[0]);
    }
}
