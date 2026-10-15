namespace MambaMQ.Core.Services.Implementations;

internal sealed partial class QueueManager
{
    public async Task<CommandResponse> PublishMessage(
        string queueName,
        MambaMessage message,
        IClientConnection connection,
        CancellationToken cancellationToken = default)
    {
        MambaQueue? queue = GetQueueByName(queueName);

        if (queue is null)
        {
            _serverLogger.LogWarning("Queue '{QueueName}' does not exist.", queueName);

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
    
    public async Task<CommandResponse> DeleteMessage(
        string queueName,
        Guid messageId,
        IClientConnection connection,
        CancellationToken cancellationToken = default)
    {
        MambaQueue? queue = GetQueueByName(queueName);

        if (queue is null)
        {
            _serverLogger.LogWarning("Queue {QueueName} does not exist.", queueName);

            return CommandResponse.Fail(ErrorCode.QueueNotFound, $"Queue '{queueName}' does not exist.");
        }

        if (!HasQueuePermission(queue, connection, QueuePermission.Delete))
        {
            mambaLogger.Queue(queue.Name).LogWarning("Service '{ServiceName}' has no Delete permission for this queue.", connection.ServiceName);

            return CommandResponse.Fail(ErrorCode.PermissionDenied, $"Service '{connection.ServiceName}' has no Delete permission for queue '{queue.Name}'.");
        }

        try
        {
            if(queue.MessageRetentionEnabled)
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

    public async Task<CommandResponse> DeleteMessages(
        string queueName, 
        ICollection<Guid> messageIds, 
        IClientConnection connection, 
        CancellationToken cancellationToken = default)
    {
        MambaQueue? queue = GetQueueByName(queueName);

        if (queue is null)
        {
            _serverLogger.LogWarning("Queue {QueueName} does not exist.", queueName);

            return CommandResponse.Fail(ErrorCode.QueueNotFound, $"Queue '{queueName}' does not exist.");
        }

        if (!HasQueuePermission(queue, connection, QueuePermission.Delete))
        {
            mambaLogger.Queue(queue.Name).LogWarning("Service '{ServiceName}' has no Delete permission for this queue.", connection.ServiceName);

            return CommandResponse.Fail(ErrorCode.PermissionDenied, $"Service '{connection.ServiceName}' has no Delete permission for queue '{queue.Name}'.");
        }

        try
        {
            if(queue.MessageRetentionEnabled)
                foreach (Guid messageId in messageIds)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    await serverStorageService.MarkAsDeleteMessage(queue.Id, messageId, cancellationToken);
                }

            queue.DeleteMessages(messageIds);

            foreach (Guid _ in messageIds)
                DecrementInFlight(queue.Id, connection.Id);

            mambaLogger.Queue(queue.Name).LogDebug("Deleted {MessageCount} messages from queue.", messageIds.Count);

            return CommandResponse.Success();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            mambaLogger.Queue(queue.Name).LogError(exception, "Failed to delete multiple messages from queue.");

            return CommandResponse.Fail(ErrorCode.PersistenceError, "Failed to delete messages.");
        }
    }
    
    public Task<CommandResponse<GetMessagesResponse>> GetMessagesAsync(
        string queueName,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (skip < 0)
            return Task.FromResult(CommandResponse<GetMessagesResponse>.Fail(ErrorCode.InvalidArgument, "Skip cannot be negative."));

        if (take is < 1 or > 20)
            return Task.FromResult(CommandResponse<GetMessagesResponse>.Fail(ErrorCode.InvalidArgument, "Page size must be between 1 and 20."));

        MambaQueue? queue = GetQueueByName(queueName);

        if (queue is null)
            return Task.FromResult(CommandResponse<GetMessagesResponse>.Fail(ErrorCode.QueueNotFound, $"Queue '{queueName}' does not exist."));

        (IReadOnlyList<MambaMessage> messages, int totalCount) = queue.GetMessages(skip, take);

        GetMessagesResponse response = new()
        {
            TotalCount = totalCount,
            Messages = messages
                .Select(message => new QueueMessageModel
                {
                    MessageId = message.MessageId,
                    ReceivedAt = message.ReceivedAt,
                    Payload = message.Body.ToArray()
                })
                .ToArray()
        };

        return Task.FromResult(CommandResponse<GetMessagesResponse>.Success(response));
    }
}