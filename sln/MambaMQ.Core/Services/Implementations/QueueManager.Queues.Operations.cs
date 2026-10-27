namespace MambaMQ.Core.Services.Implementations;

internal sealed partial class QueueManager
{
    public Task<CommandResponse> SubscribeQueue(
        string queueName,
        IClientConnection connection,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(queueName))
            return Task.FromResult(CommandResponse.Fail(ErrorCode.InvalidArgument, "Queue name cannot be empty or whitespace."));

        MambaQueue? queue = GetQueueByName(queueName);

        if (queue is null)
        {
            _serverLogger.LogWarning("Queue '{QueueName}' does not exist.", queueName);

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

    public Task<CommandResponse> SubscribeQueueWithBatch(QueueSubscribeWithBatchDto? dto, IClientConnection connection, CancellationToken cancellationToken = default)
    {
        if (dto is null)
            return Task.FromResult(CommandResponse.Fail(ErrorCode.InvalidArgument, "Batch subscription options cannot be null."));

        if (string.IsNullOrWhiteSpace(dto.QueueName))
            return Task.FromResult(CommandResponse.Fail(ErrorCode.InvalidArgument, "Queue name cannot be empty or whitespace."));

        if (dto.MaxMessages <= 0)
            return Task.FromResult(CommandResponse.Fail(ErrorCode.InvalidArgument, "Maximum messages must be greater than zero."));

        if (dto.MaxBytes <= 0)
            return Task.FromResult(CommandResponse.Fail(ErrorCode.InvalidArgument, "Maximum bytes must be greater than zero."));

        if (dto.MaxWaitTime <= TimeSpan.Zero)
            return Task.FromResult(CommandResponse.Fail(ErrorCode.InvalidArgument, "Maximum wait time must be greater than zero."));

        if (dto.Weight <= 0)
            return Task.FromResult(CommandResponse.Fail(ErrorCode.InvalidArgument, "Subscriber weight must be greater than zero."));

        MambaQueue? queue = GetQueueByName(dto.QueueName);

        if (queue is null)
        {
            _serverLogger.LogWarning("Queue '{QueueName}' does not exist.", dto.QueueName);

            return Task.FromResult(CommandResponse.Fail(ErrorCode.QueueNotFound, $"Queue '{dto.QueueName}' does not exist."));
        }

        if (!HasQueuePermission(queue, connection, QueuePermission.Read))
        {
            mambaLogger.Queue(queue.Name).LogWarning("Service '{ServiceName}' has no Read permission for this queue.", connection.ServiceName);

            return Task.FromResult(CommandResponse.Fail(ErrorCode.PermissionDenied, $"Service '{connection.ServiceName}' has no Read permission for queue '{queue.Name}'."));
        }

        Subscriber subscriber = new(connection, dto.MaxMessages, dto.MaxBytes, dto.MaxWaitTime, dto.Weight);

        AddSubscriber(queue.Id, subscriber);

        _ = ConsumeWithBatch(queue, subscriber, cancellationToken);

        StartNextBatch(queue);

        mambaLogger.Queue(queue.Name).LogInformation("Subscriber {ConnectionId} is subscribed.", subscriber.Connection.Id);

        return Task.FromResult(CommandResponse.Success());
    }
    
    public async Task RestoreQueues(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<StoredMambaQueueState> storedQueues = await serverStorageService.RestoreQueues(cancellationToken);

        _serverLogger.LogInformation("Successfully restored {QueueCount} queues.", storedQueues.Count);
        
        foreach (StoredMambaQueueState storedQueue in storedQueues)
        {
            cancellationToken.ThrowIfCancellationRequested();

            MambaQueue queue = RestoreQueue(storedQueue);

            RegisterQueue(queue);
            
            mambaLogger.Queue(queue.Name).LogInformation("Restored {MessagesCount} messages.", storedQueue.Messages.Count);
            mambaLogger.Queue(queue.Name).LogInformation("Queue was successfully restored.");
        }
    }
}