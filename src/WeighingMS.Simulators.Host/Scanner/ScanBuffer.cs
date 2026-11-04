namespace WeighingMS.Simulators.Host.Scanner;

/// <summary>
/// Ordered buffer (FIFO) of scans read by the dummy scanner. Consumers collect with a cursor (<c>after</c> = last sequence seen),
/// so collection is safe to retry: a scan is delivered once per cursor and is never lost by a failed poll.
/// </summary>
public sealed class ScanBuffer(TimeProvider clock)
{
    private readonly Lock _gate = new();
    private readonly List<ScanResponse> _scans = [];
    private long _sequence;

    /// <summary>Queues a scan and returns it with its sequence number.</summary>
    public ScanResponse Enqueue(string code)
    {
        lock (_gate)
        {
            var scan = new ScanResponse(++_sequence, code.Trim(), clock.GetUtcNow());
            _scans.Add(scan);
            return scan;
        }
    }

    /// <summary>All scans with a sequence greater than <paramref name="after"/>, in order.</summary>
    public IReadOnlyList<ScanResponse> After(long after)
    {
        lock (_gate)
        {
            return [.. _scans.Where(s => s.Sequence > after)];
        }
    }

    /// <summary>Empties the queue and restarts the sequence.</summary>
    public void Reset()
    {
        lock (_gate)
        {
            _scans.Clear();
            _sequence = 0;
        }
    }
}
