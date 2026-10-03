namespace MambaMQ.Core.Services.Abstractions;

public interface IQueueManager
{
    Task<CommandResponse> PublishMessage(string queueName, MambaMessage message, IClientConnection connection, CancellationToken cancellationToken = default);
    Task<CommandResponse> SubscribeQueue(string queueName, IClientConnection connection, CancellationToken cancellationToken = default);
    Task<CommandResponse> DeleteMessage(string queueName, Guid messageId, IClientConnection connection, CancellationToken cancellationToken = default);
    Task RestoreQueues(CancellationToken cancellationToken = default);

    Task<CommandResponse> CreateQueue(
        string queueName, 
        bool isDurable, 
        LoadBalancingAlgorithm loadBalancingAlgorithm,
        Dictionary<string, QueuePermission> permissions,
        bool messageRetentionEnabled,
        TimeSpan messageRetentionPeriod);

    Task<CommandResponse> SubscribeQueueWithBatch(
        string queueName,
        IClientConnection connection,
        int maxMessages,
        int maxBytes,
        TimeSpan maxWaitTime,
        int weight,
        CancellationToken cancellationToken = default);
}