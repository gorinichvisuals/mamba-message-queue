namespace MambaMQ.Server.Recovery;

public interface IQueueRecoveryService
{
    Task RestoreQueues(CancellationToken cancellationToken = default);
}