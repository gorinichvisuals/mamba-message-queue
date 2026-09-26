namespace MambaMQ.Server.Logging;

internal sealed class QueueLogService(IQueueStorageService queueStorageService) : IQueueLogService
{
    public async Task Log(
        MambaQueue queue, 
        LogLevel level,
        LogEventType eventType,
        string message,
        CancellationToken cancellationToken = default)
    {
        if (!queue.LogRetentionEnabled)
            return;

        if ((queue.LogLevel & level) is 0)
            return;

        StoredQueueLog log = new(DateTimeOffset.UtcNow, (byte)level, (byte)eventType, message);

        await queueStorageService.SaveLog(queue.Id, log, cancellationToken);
    }
}