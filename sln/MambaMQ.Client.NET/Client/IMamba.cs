namespace MambaMQ.Client.NET;

public interface IMamba
{
    Task<ExchangeModel> CreateExchangeAsync(ExchangeCreateOptions exchangeCreateOptions, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExchangeModel>> GetExchangesAsync(CancellationToken cancellationToken = default);
    Task<ExchangeModel> UpdateExchangeAsync(Guid exchangeId, ExchangeCreateOptions exchangeCreateOptions, CancellationToken cancellationToken = default);
    Task DeleteExchangeAsync(Guid exchangeId, CancellationToken cancellationToken = default);
    Task BindQueueToExchangeAsync(string exchangeName, string queueName, string routingKey, CancellationToken cancellationToken = default);
    Task UnbindQueueFromExchangeAsync(string exchangeName, string queueName, string routingKey, CancellationToken cancellationToken = default);
    Task PublishToExchangeAsync<T>(string exchangeName, string routingKey, T message, CancellationToken cancellationToken = default);
    
    Task<QueueModel> CreateQueueAsync(QueueCreateOptions queueCreateOptions, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<QueueModel>> GetQueuesAsync(CancellationToken cancellationToken = default);
    Task<QueueModel> UpdateQueueAsync(Guid queueId, QueueCreateOptions queueCreateOptions, CancellationToken cancellationToken = default);
    Task DeleteQueueAsync(Guid queueId, CancellationToken cancellationToken = default);
    Task PublishToQueueAsync<T>(string queueName, T message, CancellationToken cancellationToken = default);
    IAsyncEnumerable<MambaMessage> SubscribeQueueAsync(string queueName, CancellationToken cancellationToken = default);
    IAsyncEnumerable<IReadOnlyList<MambaMessage>> SubscribeQueueWithBatchAsync(string queueName, BatchSubscribeOptions options, CancellationToken cancellationToken = default);
    Task DeleteMessageAsync(string queueName, Guid messageId, CancellationToken cancellationToken = default);
    Task<GetMessagesResponse> GetMessagesAsync(string queueName, int skip, int take, CancellationToken cancellationToken = default);
    Task DeleteMessagesAsync(string queueName, ICollection<Guid> messageIds, CancellationToken cancellationToken = default);
}