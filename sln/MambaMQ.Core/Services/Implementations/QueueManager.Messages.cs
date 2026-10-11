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
}