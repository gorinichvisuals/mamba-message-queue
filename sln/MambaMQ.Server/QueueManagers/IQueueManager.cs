namespace MambaMQ.Server.QueueManagers;

public interface IQueueManager
{
    Task PublishMessageAsync(string queueName, MambaMessage message, CancellationToken cancellationToken = default);
    Task SubscribeQueueAsync(string queueName, bool autoAcknowledge, IClientConnection connection, CancellationToken cancellationToken = default);    
    Task DeleteMessageAsync(string queueName, Guid messageId, CancellationToken cancellationToken = default);
}