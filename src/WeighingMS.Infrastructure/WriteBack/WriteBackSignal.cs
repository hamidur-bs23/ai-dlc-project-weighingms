using WeighingMS.Application.WriteBack;

namespace WeighingMS.Infrastructure.WriteBack;

/// <summary>In-process wake-up for the dispatcher: a completion releases it; repeated notifications collapse into one.</summary>
public sealed class WriteBackSignal : IWriteBackSignal, IDisposable
{
    private readonly SemaphoreSlim _signal = new(0, 1);

    /// <inheritdoc />
    public void Notify()
    {
        if (_signal.CurrentCount == 0)
        {
            try
            {
                _signal.Release();
            }
            catch (SemaphoreFullException)
            {
                // Already signalled.
            }
        }
    }

    /// <summary>Waits for a notification or the timeout; true when notified.</summary>
    public Task<bool> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken) => _signal.WaitAsync(timeout, cancellationToken);

    /// <inheritdoc />
    public void Dispose() => _signal.Dispose();
}
