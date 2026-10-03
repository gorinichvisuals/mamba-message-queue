namespace MambaMQ.Persistence.Services.Abstractions;

public interface IServerStorageService
{
    Task SaveExchange(StoredMambaExchange storedExchange, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<StoredMambaExchange>> RestoreExchanges(CancellationToken cancellationToken = default);
    Task DeleteExchange(Guid exchangeId, CancellationToken cancellationToken = default);
    
    Task SaveQueue(StoredMambaQueue storedQueue, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<StoredMambaQueueState>> RestoreQueues(CancellationToken cancellationToken = default);
    Task DeleteQueue(Guid queueId, CancellationToken cancellationToken = default);
    
    Task SaveMessage(Guid queueId, StoredMambaMessage message, CancellationToken cancellationToken = default);
    Task MarkAsDeleteMessage(Guid queueId, Guid messageId, CancellationToken cancellationToken = default);
    
    Task CleanupMessages(CancellationToken cancellationToken = default);
}