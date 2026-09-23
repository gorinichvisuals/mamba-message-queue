namespace MambaMQ.Server.QueueManagers;

public interface IQueueManager
{
    Task PublishMessage(string queueName, bool isDurable, bool persistMessages, 
        MambaMessage message, CancellationToken cancellationToken = default);
    
    Task SubscribeQueue(string queueName, bool isDurable, bool persistMessages, 
        IClientConnection connection, CancellationToken cancellationToken = default);
    
    Task DeleteMessage(string queueName, Guid messageId, CancellationToken cancellationToken = default);
    
    Task RestoreQueues(CancellationToken cancellationToken = default);
}