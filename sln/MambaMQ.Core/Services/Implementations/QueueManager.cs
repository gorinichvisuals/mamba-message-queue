namespace MambaMQ.Core.Services.Implementations;

internal sealed partial class QueueManager(
    bool authorizationEnabled,
    IServerStorageService serverStorageService, 
    IMambaLogger mambaLogger) : IQueueManager
{
    private readonly Dictionary<Guid, MambaQueue> _queues = [];
    private readonly Dictionary<string, Guid> _queueNames = [];
    private readonly Dictionary<Guid, List<Subscriber>> _subscribers = [];  
    private readonly Dictionary<Guid, ILoadBalancingStrategy> _loadBalancingStrategies = [];
    private readonly Dictionary<Guid, SemaphoreSlim> _batchLocks = [];

    public async Task<CommandResponse> CreateQueue(
        string queueName,
        bool isDurable,
        LoadBalancingAlgorithm loadBalancingAlgorithm,
        Dictionary<string, QueuePermission> permissions,
        bool messageRetentionEnabled,
        TimeSpan messageRetentionPeriod)
    {
        CommandResponse validationResponse = ValidateQueueOptions(isDurable, messageRetentionEnabled);

        if (!validationResponse.IsSucceed)
            return validationResponse;
        
        MambaQueue? queue = GetQueue(queueName);

        if (queue is not null)
        {
            mambaLogger.Server.LogInformation("Queue '{QueueName}' already exists.", queueName);

            return CommandResponse.Success();
        }

        MambaQueue newQueue = new(
            Guid.CreateVersion7(),
            queueName,
            isDurable,
            loadBalancingAlgorithm,
            permissions,
            messageRetentionEnabled,
            messageRetentionPeriod);

        if (isDurable)
        {
            CommandResponse response = await PersistQueue(newQueue);

            if (!response.IsSucceed)
                return response;
        }

        _queues.Add(newQueue.Id, newQueue);
        _queueNames.Add(newQueue.Name, newQueue.Id);
        _loadBalancingStrategies.Add(newQueue.Id, LoadBalancingStrategyFactory.Create(newQueue.LoadBalancingAlgorithm));
        _batchLocks.Add(newQueue.Id, new SemaphoreSlim(1, 1));

        mambaLogger.Queue(newQueue.Name).LogInformation("Queue was created.");

        return CommandResponse.Success();
    }

    public async Task<CommandResponse> PublishMessage(
        string queueName,
        MambaMessage message,
        IClientConnection connection,
        CancellationToken cancellationToken = default)
    {
        MambaQueue? queue = GetQueue(queueName);

        if (queue is null)
        {
            mambaLogger.Server.LogWarning("Queue '{QueueName}' does not exist.", queueName);

            return CommandResponse.Fail(ErrorCode.QueueNotFound, $"Queue '{queueName}' does not exist.");
        }

        if (!HasQueuePermission(queue, connection, QueuePermission.Write))
        {
            mambaLogger.Queue(queue.Name).LogWarning("Service '{ServiceName}' has no Write permission for this queue.", connection.ServiceName);

            return CommandResponse.Fail(ErrorCode.PermissionDenied, $"Service '{connection.ServiceName}' has no Write permission for queue '{queue.Name}'.");
        }

        if (queue.MessageRetentionEnabled)
        {
            CommandResponse response = await PersistMessageIfRequired(queue, message, cancellationToken);
            
            if (!response.IsSucceed)
                return response;
        }

        queue.PublishMessage(message);

        mambaLogger.Queue(queue.Name).LogDebug("Message {MessageId} has been published to queue.", message.MessageId);

        return CommandResponse.Success();
    }

    public Task<CommandResponse> SubscribeQueue(
        string queueName,
        IClientConnection connection,
        CancellationToken cancellationToken = default)
    {
        MambaQueue? queue = GetQueue(queueName);

        if (queue is null)
        {
            mambaLogger.Server.LogWarning("Queue '{QueueName}' does not exist.", queueName);

            return Task.FromResult(CommandResponse.Fail(ErrorCode.QueueNotFound, $"Queue '{queueName}' does not exist."));
        }

        if (!HasQueuePermission(queue, connection, QueuePermission.Read))
        {
            mambaLogger.Queue(queue.Name).LogWarning("Service '{ServiceName}' has no Read permission for this queue.", connection.ServiceName);

            return Task.FromResult(CommandResponse.Fail(ErrorCode.PermissionDenied, $"Service '{connection.ServiceName}' has no Read permission for queue '{queue.Name}'."));
        }

        _ = Consume(queue, connection, cancellationToken);

        return Task.FromResult(CommandResponse.Success());
    }

    public Task<CommandResponse> SubscribeQueueWithBatch(
        string queueName,
        IClientConnection connection,
        int maxMessages,
        int maxBytes,
        TimeSpan maxWaitTime,
        int weight,
        CancellationToken cancellationToken = default)
    {
        MambaQueue? queue = GetQueue(queueName);

        if (queue is null)
        {
            mambaLogger.Server.LogWarning("Queue '{QueueName}' does not exist.", queueName);

            return Task.FromResult(CommandResponse.Fail(ErrorCode.QueueNotFound, $"Queue '{queueName}' does not exist."));
        }

        if (!HasQueuePermission(
                queue, connection, QueuePermission.Read))
        {
            mambaLogger.Queue(queue.Name).LogWarning("Service '{ServiceName}' has no Read permission for this queue.", connection.ServiceName);

            return Task.FromResult(CommandResponse.Fail(ErrorCode.PermissionDenied, $"Service '{connection.ServiceName}' has no Read permission for queue '{queue.Name}'."));
        }

        Subscriber subscriber = new(
            connection,
            maxMessages,
            maxBytes,
            maxWaitTime,
            weight);

        AddSubscriber(queue.Id, subscriber);

        _ = ConsumeWithBatch(queue, subscriber, cancellationToken);

        StartNextBatch(queue);

        mambaLogger.Queue(queue.Name).LogInformation("Subscriber {ConnectionId} is subscribed.", subscriber.Connection.Id);

        return Task.FromResult(CommandResponse.Success());
    }
    
    public async Task<CommandResponse> DeleteMessage(
        string queueName,
        Guid messageId,
        IClientConnection connection,
        CancellationToken cancellationToken = default)
    {
        MambaQueue? queue = GetQueue(queueName);

        if (queue is null)
        {
            mambaLogger.Server.LogWarning("Queue {QueueName} does not exist.", queueName);

            return CommandResponse.Fail(ErrorCode.QueueNotFound, $"Queue '{queueName}' does not exist.");
        }

        if (!HasQueuePermission(queue, connection, QueuePermission.Delete))
        {
            mambaLogger.Queue(queue.Name).LogWarning("Service '{ServiceName}' has no Delete permission for this queue.", connection.ServiceName);

            return CommandResponse.Fail(ErrorCode.PermissionDenied, $"Service '{connection.ServiceName}' has no Delete permission for queue '{queue.Name}'.");
        }

        try
        {
            await serverStorageService.MarkAsDeleteMessage(queue.Id, messageId, cancellationToken);
        }
        catch (Exception exception)
        {
            mambaLogger.Queue(queue.Name).LogError(exception, "Failed to delete message {MessageId}.", messageId);

            return CommandResponse.Fail(ErrorCode.PersistenceError, "Failed to delete message.");
        }

        queue.DeleteMessage(messageId);

        DecrementInFlight(queue.Id, connection.Id);

        mambaLogger.Queue(queue.Name).LogDebug("Message {MessageId} has been deleted.", messageId);

        return CommandResponse.Success();
    }

    public async Task RestoreQueues(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<StoredMambaQueueState> storedQueues = await serverStorageService.RestoreQueues(cancellationToken);

        mambaLogger.Server.LogInformation("Successfully restored {QueueCount} queues.", storedQueues.Count);
        
        foreach (StoredMambaQueueState storedQueue in storedQueues)
        {
            cancellationToken.ThrowIfCancellationRequested();

            MambaQueue queue = new(
                storedQueue.Queue.Id, 
                storedQueue.Queue.Name,
                storedQueue.Queue.IsDurable, 
                (LoadBalancingAlgorithm)storedQueue.Queue.LoadBalancingAlgorithm,
                storedQueue.Queue.Permissions.ToDictionary(
                    x => x.Key,
                    x => (QueuePermission)x.Value),
                storedQueue.Queue.MessageRetention.RetentionEnabled,
                storedQueue.Queue.MessageRetention.RetentionPeriod);

            foreach (StoredMambaMessage message in storedQueue.Messages)
                queue.PublishMessage(new MambaMessage(message.Id, message.ReceivedAt, message.Body));
            
            _queues.Add(queue.Id, queue);
            _queueNames.Add(queue.Name, queue.Id);
            _loadBalancingStrategies.Add(queue.Id, LoadBalancingStrategyFactory.Create(queue.LoadBalancingAlgorithm));
            _batchLocks.Add(queue.Id, new SemaphoreSlim(1, 1));
            
            mambaLogger.Queue(queue.Name).LogInformation("Restored {MessagesCount} messages.", storedQueue.Messages.Count);
            mambaLogger.Queue(queue.Name).LogInformation("Queue was successfully restored.");
        }
    }
}