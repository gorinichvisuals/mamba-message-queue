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

    public async Task<CommandResponse<QueueModel>> CreateQueue(
        string queueName,
        bool isDurable,
        LoadBalancingAlgorithm loadBalancingAlgorithm,
        Dictionary<string, QueuePermission> permissions,
        bool messageRetentionEnabled,
        TimeSpan messageRetentionPeriod)
    {
        CommandResponse validationResponse = ValidateQueueOptions(isDurable, messageRetentionEnabled);

        if (!validationResponse.IsSucceed)
            return CommandResponse<QueueModel>.Fail(validationResponse.ErrorCode, validationResponse.ErrorMessage ?? "Queue validation failed.");

        MambaQueue? queue = GetQueueByName(queueName);

        if (queue is not null)
        {
            mambaLogger.Server.LogInformation("Queue '{QueueName}' already exists.", queueName);

            return CommandResponse<QueueModel>.Success(ToQueueModel(queue));
        }

        MambaQueue newQueue = new(
            Guid.CreateVersion7(),
            queueName,
            isDurable,
            DateTimeOffset.UtcNow,
            loadBalancingAlgorithm,
            permissions,
            messageRetentionEnabled,
            messageRetentionPeriod);

        if (isDurable)
        {
            CommandResponse response = await PersistQueue(newQueue);

            if (!response.IsSucceed)
                return CommandResponse<QueueModel>.Fail(response.ErrorCode, response.ErrorMessage ?? "Failed to persist queue.");
        }

        RegisterQueue(newQueue);

        mambaLogger.Queue(newQueue.Name)
            .LogInformation("Queue was created.");

        return CommandResponse<QueueModel>.Success(ToQueueModel(newQueue));
    }

    public async Task<CommandResponse> PublishMessage(
        string queueName,
        MambaMessage message,
        IClientConnection connection,
        CancellationToken cancellationToken = default)
    {
        MambaQueue? queue = GetQueueByName(queueName);

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
        MambaQueue? queue = GetQueueByName(queueName);

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
        MambaQueue? queue = GetQueueByName(queueName);

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

    public async Task<CommandResponse<QueueModel>> UpdateQueue(
        Guid queueId,
        string queueName,
        bool isDurable,
        LoadBalancingAlgorithm loadBalancingAlgorithm,
        Dictionary<string, QueuePermission> permissions,
        bool messageRetentionEnabled,
        TimeSpan messageRetentionPeriod,
        CancellationToken cancellationToken = default)
    {
        CommandResponse validationResponse = ValidateQueueOptions(isDurable, messageRetentionEnabled);

        if (!validationResponse.IsSucceed)
            return CommandResponse<QueueModel>.Fail(validationResponse.ErrorCode, validationResponse.ErrorMessage ?? "Queue validation failed.");

        MambaQueue? queue = GetQueueById(queueId);

        if (queue is null)
        {
            mambaLogger.Server.LogWarning("Queue '{QueueId}' does not exist.", queueId);

            return CommandResponse<QueueModel>.Fail(ErrorCode.QueueNotFound, $"Queue '{queueId}' does not exist.");
        }

        MambaQueue? queueWithSameName = GetQueueByName(queueName);

        if (queueWithSameName is not null && queueWithSameName.Id != queueId)
        {
            mambaLogger.Server.LogWarning("Queue '{QueueName}' already exists.", queueName);

            return CommandResponse<QueueModel>.Fail(ErrorCode.QueueAlreadyExists, $"Queue '{queueName}' already exists.");
        }

        string previousQueueName = queue.Name;
        LoadBalancingAlgorithm previousLoadBalancingAlgorithm = queue.LoadBalancingAlgorithm;
        
        if (queue.IsDurable && !isDurable)
        {
            try
            {
                await serverStorageService.DeleteQueue(queue.Id, cancellationToken);
            }
            catch (Exception exception)
            {
                mambaLogger.Queue(queue.Name).LogError(exception, "Failed to delete queue.");

                return CommandResponse<QueueModel>.Fail(ErrorCode.PersistenceError, "Failed to delete queue.");
            }
        }
        
        queue.Update(
            queueName,
            isDurable,
            loadBalancingAlgorithm,
            permissions,
            messageRetentionEnabled,
            messageRetentionPeriod);

        if (previousQueueName != queueName)
        {
            _queueNames.Remove(previousQueueName);
            _queueNames[queueName] = queueId;
        }

        if (previousLoadBalancingAlgorithm != loadBalancingAlgorithm)
            _loadBalancingStrategies[queueId] = LoadBalancingStrategyFactory.Create(loadBalancingAlgorithm);

        if (isDurable)
        {
            CommandResponse response = await PersistQueue(queue, cancellationToken);

            if (!response.IsSucceed)
                return CommandResponse<QueueModel>.Fail(response.ErrorCode, response.ErrorMessage ?? "Failed to persist queue.");
        }

        mambaLogger.Queue(queue.Name).LogInformation("Queue was updated.");

        return CommandResponse<QueueModel>.Success(ToQueueModel(queue));
    }

    public async Task<CommandResponse> DeleteMessage(
        string queueName,
        Guid messageId,
        IClientConnection connection,
        CancellationToken cancellationToken = default)
    {
        MambaQueue? queue = GetQueueByName(queueName);

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

            MambaQueue queue = RestoreQueue(storedQueue);

            RegisterQueue(queue);
            
            mambaLogger.Queue(queue.Name).LogInformation("Restored {MessagesCount} messages.", storedQueue.Messages.Count);
            mambaLogger.Queue(queue.Name).LogInformation("Queue was successfully restored.");
        }
    }
    
    public IReadOnlyList<QueueModel> GetQueues()
    {
        return _queues.Values
            .OrderByDescending(queue => queue.CreatedAt)
            .Select(queue => new QueueModel
            {
                Id = queue.Id,
                Name = queue.Name,
                IsDurable = queue.IsDurable,
                LoadBalancingAlgorithm = queue.LoadBalancingAlgorithm,
                Permissions = new Dictionary<string, QueuePermission>(queue.Permissions),
                MessageRetentionEnabled = queue.MessageRetentionEnabled,
                MessageRetentionPeriod = queue.MessageRetentionPeriod,
                CreatedAt = queue.CreatedAt,
                AvailableMessageCount = queue.AvailableMessageCount,
                InFlightMessageCount = queue.InFlightMessageCount
            })
            .ToList();
    }
    
    public async Task<CommandResponse> DeleteQueue(Guid queueId, IClientConnection connection, CancellationToken cancellationToken = default)
    {
        if (!_queues.TryGetValue(queueId, out MambaQueue? queue))
        {
            mambaLogger.Server.LogWarning("Queue '{QueueId}' does not exist.", queueId);

            return CommandResponse.Fail(ErrorCode.QueueNotFound, $"Queue '{queueId}' does not exist.");
        }

        if (!HasQueuePermission(queue, connection, QueuePermission.Delete))
        {
            mambaLogger.Queue(queue.Name).LogWarning("Service '{ServiceName}' has no Delete permission for this queue.", connection.ServiceName);

            return CommandResponse.Fail(ErrorCode.PermissionDenied, $"Service '{connection.ServiceName}' has no Delete permission for queue '{queue.Name}'.");
        }

        if (queue.IsDurable)
        {
            try
            {
                await serverStorageService.DeleteQueue(queue.Id, cancellationToken);
            }
            catch (Exception exception)
            {
                mambaLogger.Queue(queue.Name).LogError(exception, "Failed to delete queue.");

                return CommandResponse.Fail(ErrorCode.PersistenceError, "Failed to delete queue.");
            }
        }

        _queues.Remove(queue.Id);
        _queueNames.Remove(queue.Name);

        _subscribers.Remove(queue.Id);
        _loadBalancingStrategies.Remove(queue.Id);

        if (_batchLocks.Remove(queue.Id, out SemaphoreSlim? batchLock))
            batchLock.Dispose();

        mambaLogger.Queue(queue.Name)
            .LogInformation("Queue was deleted.");

        return CommandResponse.Success();
    }
}