namespace MambaMQ.Persistence.Services.Abstractions;

public interface IServerStorageService
{
    Task SaveQueue(StoredMambaQueue storedQueue, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<StoredMambaQueueState>> RestoreQueues(CancellationToken cancellationToken = default);
    Task DeleteQueue(Guid queueId, CancellationToken cancellationToken = default);
    
    Task SaveMessage(Guid queueId, StoredMambaMessage message, CancellationToken cancellationToken = default);
    Task MarkAsDeleteMessage(Guid queueId, Guid messageId, CancellationToken cancellationToken = default);
    
    Task SaveQueueLog(Guid queueId, StoredQueueLog log, CancellationToken cancellationToken = default);
    Task WriteServerLog(string message, CancellationToken cancellationToken = default);
    
    Task CleanupMessages(CancellationToken cancellationToken = default);
    Task CleanupQueueLogs(CancellationToken cancellationToken = default);
    Task CleanupServerLogs(TimeSpan retentionPeriod, CancellationToken cancellationToken = default);
}