namespace MambaMQ.Client.NET;

public interface IMamba
{
    Task CreateQueueAsync(QueueOptions queueOptions, CancellationToken cancellationToken = default);
    Task PublishToQueueAsync<T>(string queueName, T message, CancellationToken cancellationToken = default);
    IAsyncEnumerable<MambaMessage> SubscribeQueueAsync(string queueName, CancellationToken cancellationToken = default);
    IAsyncEnumerable<IReadOnlyList<MambaMessage>> SubscribeQueueWithBatchAsync(string queueName, BatchSubscribeOptions options, CancellationToken cancellationToken = default);
    Task DeleteMessageAsync(string queueName, Guid messageId, CancellationToken cancellationToken = default);
    Task CreateExchangeAsync(ExchangeOptions exchangeOptions, CancellationToken cancellationToken = default);
    Task BindQueueToExchangeAsync(string exchangeName, string queueName, string routingKey, CancellationToken cancellationToken = default);
    Task UnbindQueueFromExchangeAsync(string exchangeName, string queueName, string routingKey, CancellationToken cancellationToken = default);
    Task PublishToExchangeAsync<T>(string exchangeName, string routingKey, T message, CancellationToken cancellationToken = default);
}