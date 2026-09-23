namespace MambaMQ.Persistence.Services.Abstractions;

public interface IQueueStorageService
{
    Task SaveQueue(StoredMambaQueue storedQueue, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<StoredMambaQueueState>> RestoreQueues(CancellationToken cancellationToken = default);
    Task DeleteQueue(Guid queueId, CancellationToken cancellationToken = default);
    
    Task SaveMessage(Guid queueId, StoredMambaMessage message, CancellationToken cancellationToken = default);
    Task DeleteMessage(Guid queueId, Guid messageId, CancellationToken cancellationToken = default);
    
    Task Cleanup(CancellationToken cancellationToken = default);
}