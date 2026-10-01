namespace MambaMQ.Client.NET;

public interface IMamba
{
    Task CreateQueueAsync(QueueOptions queueOptions, CancellationToken cancellationToken = default);
    Task PublishAsync<T>(string queueName, T message, CancellationToken cancellationToken = default);
    IAsyncEnumerable<MambaMessage> SubscribeQueueAsync(string queueName, CancellationToken cancellationToken = default);
    IAsyncEnumerable<IReadOnlyList<MambaMessage>> SubscribeWithBatchAsync(string queueName, BatchSubscribeOptions options, CancellationToken cancellationToken = default);
    Task DeleteMessageAsync(string queueName, Guid messageId, CancellationToken cancellationToken = default);
}