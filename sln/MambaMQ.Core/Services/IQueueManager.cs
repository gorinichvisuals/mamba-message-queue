namespace MambaMQ.Core.Services;

public interface IQueueManager
{
    Task PublishMessage(string queueName, MambaMessage message, CancellationToken cancellationToken = default);
    Task SubscribeQueue(string queueName, IClientConnection connection, CancellationToken cancellationToken = default);
    Task DeleteMessage(string queueName, Guid messageId, Guid connectionId, CancellationToken cancellationToken = default);
    Task RestoreQueues(CancellationToken cancellationToken = default);
    Task CreateQueue(
        string queueName, 
        bool isDurable, 
        LoadBalancingAlgorithm loadBalancingAlgorithm,
        bool messageRetentionEnabled,
        TimeSpan messageRetentionPeriod);

    Task SubscribeQueueWithBatch(
        string queueName,
        IClientConnection connection,
        int maxMessages,
        int maxBytes,
        TimeSpan maxWaitTime,
        int weight,
        CancellationToken cancellationToken = default);
}