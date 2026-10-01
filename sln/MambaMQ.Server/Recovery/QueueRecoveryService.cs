namespace MambaMQ.Server.Recovery;

public sealed class QueueRecoveryService(IQueueManager queueManager) : IQueueRecoveryService
{
    public async Task RestoreQueues(CancellationToken cancellationToken = default)
        => await queueManager.RestoreQueues(cancellationToken);
}