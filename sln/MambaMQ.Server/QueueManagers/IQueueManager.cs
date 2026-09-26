namespace MambaMQ.Server.QueueManagers;

public interface IQueueManager
{
    Task PublishMessage(string queueName, MambaMessage message, CancellationToken cancellationToken = default);
    Task SubscribeQueue(string queueName, IClientConnection connection, CancellationToken cancellationToken = default);
    Task DeleteMessage(string queueName, Guid messageId, CancellationToken cancellationToken = default);
    Task RestoreQueues(CancellationToken cancellationToken = default);
    Task CreateQueue(
        string queueName, 
        bool isDurable, 
        bool messageRetentionEnabled,
        TimeSpan messageRetentionPeriod,
        bool logRetentionEnabled, 
        LogLevel logRetentionLevel, 
        TimeSpan logsRetentionPeriod);
}