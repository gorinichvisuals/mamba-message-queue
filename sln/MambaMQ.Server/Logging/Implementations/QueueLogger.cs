namespace MambaMQ.Server.Logging.Implementations;

internal sealed class QueueLogger(IServerStorageService serverStorageService) : IQueueLogger
{
    public async Task Log(
        MambaQueue queue, 
        MambaServerLogLevel level,
        LogEventType eventType,
        string message,
        CancellationToken cancellationToken = default)
    {
        if (!queue.LogRetentionEnabled)
            return;

        if ((queue.MambaServerLogLevel & level) is 0)
            return;

        StoredQueueLog log = new(DateTimeOffset.UtcNow, (byte)level, (byte)eventType, message);

        await serverStorageService.SaveQueueLog(queue.Id, log, cancellationToken);
    }
}