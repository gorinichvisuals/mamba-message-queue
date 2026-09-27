namespace MambaMQ.Server.Recovery;

public sealed class QueueRecoveryService(IQueueManager queueManager) : IQueueRecoveryService
{
    public Task RestoreQueues(CancellationToken cancellationToken = default)
        => queueManager.RestoreQueues(cancellationToken);
}