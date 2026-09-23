namespace MambaMQ.Server.Recovery;

public interface IQueueRecoveryService
{
    Task RecoverAsync(CancellationToken cancellationToken = default);
}