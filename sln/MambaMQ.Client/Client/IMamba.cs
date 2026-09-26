namespace MambaMQ.Client;

public interface IMamba
{
    Task CreateQueueAsync(QueueOptions queueOptions, CancellationToken cancellationToken = default);
    Task PublishAsync<T>(string queueName, T message, CancellationToken cancellationToken = default);
    IAsyncEnumerable<MambaMessage> SubscribeAsync(string queueName, CancellationToken cancellationToken = default);
    Task DeleteMessageAsync(string queueName, Guid messageId, CancellationToken cancellationToken = default);
}