namespace MambaMQ.Server.Recovery;

public sealed class QueueRecoveryService(
    IQueueManager queueManager, 
    IExchangeManager exchangeManager) : IQueueRecoveryService
{
    public async Task RestoreQueues(CancellationToken cancellationToken = default)
        => await Task.WhenAll(
            exchangeManager.RestoreExchanges(cancellationToken), 
            queueManager.RestoreQueues(cancellationToken));
}