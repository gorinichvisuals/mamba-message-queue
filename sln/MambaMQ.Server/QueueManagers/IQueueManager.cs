namespace MambaMQ.Server.QueueManagers;

public interface IQueueManager
{
    Task PublishMessageAsync(string queueName, MambaMessage message, CancellationToken cancellationToken = default);
    IAsyncEnumerable<MessageDelivery> SubscribeQueueAsync(string queueName, bool autoAcknowledge, Guid connectionId, CancellationToken cancellationToken = default);
    Task DeleteMessageAsync(string queueName, Guid messageId, CancellationToken cancellationToken = default);
    int RemoveExpiredMessages(DateTimeOffset now);
}