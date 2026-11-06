using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WeighingMS.Application.WriteBack;

namespace WeighingMS.Infrastructure.WriteBack;

/// <summary>
/// The background dispatcher (PD-01): wakes on every completion and every <see cref="WriteBackOptions.PollInterval"/>,
/// then posts the due write-backs. It resumes pending entries after a restart. Off when <c>WriteBack:Enabled</c> is false
/// (integration tests drive <see cref="WriteBackDispatcher"/> directly).
/// </summary>
public sealed partial class WriteBackHostedService(
    IServiceScopeFactory scopes,
    WriteBackSignal signal,
    WriteBackOptions options,
    ILogger<WriteBackHostedService> logger) : BackgroundService
{
    /// <summary>After a wake-up, wait a moment so the completing transaction has committed.</summary>
    public static readonly TimeSpan SettleDelay = TimeSpan.FromMilliseconds(250);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Enabled)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (await signal.WaitAsync(options.PollInterval, stoppingToken))
                {
                    await Task.Delay(SettleDelay, stoppingToken);
                }

                await using var scope = scopes.CreateAsyncScope();
                var result = await scope.ServiceProvider.GetRequiredService<WriteBackDispatcher>().DispatchDueAsync(stoppingToken);
                if (result.Failed > 0)
                {
                    LogSyncFailed(result.Failed);
                }
                else if (result.Retrying > 0)
                {
                    LogRetrying(result.Retrying);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                LogDispatchFailed(ex);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "{Count} OrgaTEX write-back(s) reached sync failed and need a manual retry")]
    private partial void LogSyncFailed(int count);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{Count} OrgaTEX write-back(s) failed and will be retried")]
    private partial void LogRetrying(int count);

    [LoggerMessage(Level = LogLevel.Error, Message = "The OrgaTEX write-back dispatcher failed; it will try again")]
    private partial void LogDispatchFailed(Exception exception);
}
