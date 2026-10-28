namespace MambaMQ.Client.NET;

/// <summary>Provides operations for managing exchanges, queues, and messages on a MambaMQ server.</summary>
public interface IMamba
{
    /// <summary>Creates an exchange.</summary>
    /// <param name="exchangeCreateOptions">The exchange settings.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns>The created exchange.</returns>
    Task<ExchangeModel> CreateExchangeAsync(ExchangeCreateOptions exchangeCreateOptions, CancellationToken cancellationToken = default);

    /// <summary>Gets all exchanges visible to the connected client.</summary>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns>The exchanges on the server.</returns>
    Task<IReadOnlyList<ExchangeModel>> GetExchangesAsync(CancellationToken cancellationToken = default);

    /// <summary>Updates an existing exchange.</summary>
    /// <param name="exchangeId">The ID of the exchange to update.</param>
    /// <param name="exchangeCreateOptions">The new exchange settings.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns>The updated exchange.</returns>
    Task<ExchangeModel> UpdateExchangeAsync(Guid exchangeId, ExchangeCreateOptions exchangeCreateOptions, CancellationToken cancellationToken = default);

    /// <summary>Deletes an exchange.</summary>
    /// <param name="exchangeId">The ID of the exchange to delete.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    Task DeleteExchangeAsync(Guid exchangeId, CancellationToken cancellationToken = default);

    /// <summary>Binds a queue to an exchange using a routing key.</summary>
    /// <param name="exchangeName">The exchange name.</param>
    /// <param name="queueName">The queue name.</param>
    /// <param name="routingKey">The routing key for the binding.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    Task BindQueueToExchangeAsync(string exchangeName, string queueName, string routingKey, CancellationToken cancellationToken = default);

    /// <summary>Removes a queue binding from an exchange.</summary>
    /// <param name="exchangeName">The exchange name.</param>
    /// <param name="queueName">The queue name.</param>
    /// <param name="routingKey">The routing key of the binding to remove.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    Task UnbindQueueFromExchangeAsync(string exchangeName, string queueName, string routingKey, CancellationToken cancellationToken = default);

    /// <summary>Serializes a message as JSON and publishes it to an exchange.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="exchangeName">The exchange name.</param>
    /// <param name="routingKey">The routing key used to route the message.</param>
    /// <param name="message">The message to publish.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    Task PublishToExchangeAsync<T>(string exchangeName, string routingKey, T message, CancellationToken cancellationToken = default);


    /// <summary>Creates a queue.</summary>
    /// <param name="queueCreateOptions">The queue settings.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns>The created queue.</returns>
    Task<QueueModel> CreateQueueAsync(QueueCreateOptions queueCreateOptions, CancellationToken cancellationToken = default);

    /// <summary>Gets all queues visible to the connected client.</summary>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns>The queues on the server.</returns>
    Task<IReadOnlyList<QueueModel>> GetQueuesAsync(CancellationToken cancellationToken = default);

    /// <summary>Updates an existing queue.</summary>
    /// <param name="queueId">The ID of the queue to update.</param>
    /// <param name="queueCreateOptions">The new queue settings.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns>The updated queue.</returns>
    Task<QueueModel> UpdateQueueAsync(Guid queueId, QueueCreateOptions queueCreateOptions, CancellationToken cancellationToken = default);

    /// <summary>Deletes a queue.</summary>
    /// <param name="queueId">The ID of the queue to delete.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    Task DeleteQueueAsync(Guid queueId, CancellationToken cancellationToken = default);

    /// <summary>Serializes a message as JSON and publishes it directly to a queue.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="queueName">The destination queue name.</param>
    /// <param name="message">The message to publish.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    Task PublishToQueueAsync<T>(string queueName, T message, CancellationToken cancellationToken = default);

    /// <summary>Subscribes to a queue and yields messages asynchronously as they arrive.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="cancellationToken">A token that stops the subscription.</param>
    /// <returns>An asynchronous stream of delivered messages.</returns>
    IAsyncEnumerable<MambaMessage> SubscribeQueueAsync(string queueName, CancellationToken cancellationToken = default);

    /// <summary>Subscribes to a queue and yields batches of messages asynchronously.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="options">The batch size, wait time, and consumer weight settings.</param>
    /// <param name="cancellationToken">A token that stops the subscription.</param>
    /// <returns>An asynchronous stream of message batches.</returns>
    IAsyncEnumerable<IReadOnlyList<MambaMessage>> SubscribeQueueWithBatchAsync(string queueName, BatchSubscribeOptions options, CancellationToken cancellationToken = default);

    /// <summary>Deletes one message from a queue.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="messageId">The ID of the message to delete.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    Task DeleteMessageAsync(string queueName, Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>Gets a page of messages from a queue.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="skip">The number of messages to skip.</param>
    /// <param name="take">The number of messages to return (1 to 20).</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns>The requested page of messages.</returns>
    Task<GetMessagesResponse> GetMessagesAsync(string queueName, int skip, int take, CancellationToken cancellationToken = default);

    /// <summary>Deletes multiple messages from a queue.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="messageIds">The non-empty collection of message IDs to delete.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    Task DeleteMessagesAsync(string queueName, ICollection<Guid> messageIds, CancellationToken cancellationToken = default);
}