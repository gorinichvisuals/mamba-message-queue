namespace MambaMQ.Core.Services.Implementations;

internal sealed partial class QueueManager
{
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
    
    public async Task<CommandResponse<QueueModel>> CreateQueue(QueueCreateDto? dto)
    {
        if (dto is null)
            return CommandResponse<QueueModel>.Fail(ErrorCode.InvalidArgument, "Queue options cannot be null.");

        CommandResponse validationResponse = ValidateQueueOptions(
            dto.QueueName, dto.IsDurable, dto.LoadBalancingAlgorithm, dto.Permissions,
            dto.MessageRetentionEnabled, dto.MessageRetentionPeriod);

        if (!validationResponse.IsSucceed)
            return CommandResponse<QueueModel>.Fail(validationResponse.ErrorCode, validationResponse.ErrorMessage ?? "Queue validation failed.");

        MambaQueue? queue = GetQueueByName(dto.QueueName);

        if (queue is not null)
        {
            _serverLogger.LogInformation("Queue '{QueueName}' already exists.", dto.QueueName);

            return CommandResponse<QueueModel>.Success(MapToQueueModel(queue));
        }

        MambaQueue newQueue = new(
            Guid.CreateVersion7(),
            dto.QueueName,
            dto.IsDurable,
            DateTimeOffset.UtcNow,
            dto.LoadBalancingAlgorithm,
            dto.Permissions,
            dto.MessageRetentionEnabled,
            dto.MessageRetentionPeriod);

        if (dto.IsDurable)
        {
            CommandResponse response = await PersistQueue(newQueue);

            if (!response.IsSucceed)
                return CommandResponse<QueueModel>.Fail(response.ErrorCode, response.ErrorMessage ?? "Failed to persist queue.");
        }

        RegisterQueue(newQueue);

        mambaLogger.Queue(newQueue.Name).LogInformation("Queue was created.");

        return CommandResponse<QueueModel>.Success(MapToQueueModel(newQueue));
    }
    
    public async Task<CommandResponse<QueueModel>> UpdateQueue(QueueUpdateDto? dto, CancellationToken cancellationToken = default)
    {
        if (dto is null)
            return CommandResponse<QueueModel>.Fail(ErrorCode.InvalidArgument, "Queue options cannot be null.");

        if (dto.QueueId == Guid.Empty)
            return CommandResponse<QueueModel>.Fail(ErrorCode.InvalidArgument, "Queue ID cannot be empty.");

        CommandResponse validationResponse = ValidateQueueOptions(
            dto.QueueName, dto.IsDurable, dto.LoadBalancingAlgorithm, dto.Permissions,
            dto.MessageRetentionEnabled, dto.MessageRetentionPeriod);

        if (!validationResponse.IsSucceed)
            return CommandResponse<QueueModel>.Fail(validationResponse.ErrorCode, validationResponse.ErrorMessage ?? "Queue validation failed.");

        MambaQueue? queue = GetQueueById(dto.QueueId);

        if (queue is null)
        {
            _serverLogger.LogWarning("Queue '{QueueName}' does not exist.", dto.QueueName);

            return CommandResponse<QueueModel>.Fail(ErrorCode.QueueNotFound, $"Queue '{dto.QueueName}' does not exist.");
        }

        MambaQueue? queueWithSameName = GetQueueByName(dto.QueueName);

        if (queueWithSameName is not null && queueWithSameName.Id != dto.QueueId)
        {
            _serverLogger.LogWarning("Queue '{QueueName}' already exists.", dto.QueueName);

            return CommandResponse<QueueModel>.Fail(ErrorCode.QueueAlreadyExists, $"Queue '{dto.QueueName}' already exists.");
        }

        string previousQueueName = queue.Name;
        LoadBalancingAlgorithm previousLoadBalancingAlgorithm = queue.LoadBalancingAlgorithm;
        
        if (queue.IsDurable && !dto.IsDurable)
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
        
        queue.Update(dto);

        if (previousQueueName != dto.QueueName)
        {
            _queueNames.Remove(previousQueueName);
            _queueNames[dto.QueueName] = dto.QueueId;
        }

        if (previousLoadBalancingAlgorithm != dto.LoadBalancingAlgorithm)
            _loadBalancingStrategies[dto.QueueId] = LoadBalancingStrategyFactory.Create(dto.LoadBalancingAlgorithm);

        if (dto.IsDurable)
        {
            CommandResponse response = await PersistQueue(queue, cancellationToken);

            if (!response.IsSucceed)
                return CommandResponse<QueueModel>.Fail(response.ErrorCode, response.ErrorMessage ?? "Failed to persist queue.");
        }

        mambaLogger.Queue(queue.Name).LogInformation("Queue was updated.");

        return CommandResponse<QueueModel>.Success(MapToQueueModel(queue));
    }
    
    public async Task<CommandResponse> DeleteQueue(Guid queueId, IClientConnection connection, CancellationToken cancellationToken = default)
    {
        if (queueId == Guid.Empty)
            return CommandResponse.Fail(ErrorCode.InvalidArgument, "Queue ID cannot be empty.");

        if (!_queues.TryGetValue(queueId, out MambaQueue? queue))
        {
            _serverLogger.LogWarning("Queue '{QueueId}' does not exist.", queueId);

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

        mambaLogger.Queue(queue.Name).LogInformation("Queue was deleted.");

        return CommandResponse.Success();
    }
}