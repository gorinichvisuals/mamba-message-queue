namespace MambaMQ.Server.Recovery;

public sealed class QueueRecoveryService(IQueueManager queueManager) : IQueueRecoveryService
{
    public Task RecoverAsync(CancellationToken cancellationToken = default)
        => queueManager.RestoreQueues(cancellationToken);
}