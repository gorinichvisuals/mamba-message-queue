namespace MambaMQ.Server.Workers;

internal sealed class MessageExpirationWorker(IQueueManager queueManager, IOptions<MambaServerOptions> options) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            queueManager.RemoveExpiredMessages(DateTimeOffset.UtcNow);

            await Task.Delay(options.Value.ExpirationCheckInterval, stoppingToken);
        }
    }
} 