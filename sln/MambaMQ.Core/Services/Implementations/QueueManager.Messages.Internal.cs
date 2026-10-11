namespace MambaMQ.Core.Services.Implementations;

internal sealed partial class QueueManager
{
    private async Task<CommandResponse> PersistMessageIfRequired(MambaQueue queue, MambaMessage message, CancellationToken cancellationToken)
    {
        StoredMambaMessage storedMessage = new(message.MessageId, message.ReceivedAt, message.Body);

        try
        {
            await serverStorageService.SaveMessage(queue.Id, storedMessage, cancellationToken);

            return CommandResponse.Success();
        }
        catch (Exception exception)
        {
            mambaLogger.Queue(queue.Name).LogError(exception, "MessageId - {MessageMessageId}", message.MessageId);

            return CommandResponse.Fail(ErrorCode.PersistenceError, "Failed to persist message.");
        }
    }
    
    private void DecrementInFlight(Guid queueId, Guid connectionId)
    {
        lock (_subscribers)
        {
            if (!_subscribers.TryGetValue(queueId, out List<Subscriber>? subscribers))
                return;

            Subscriber? subscriber = subscribers.FirstOrDefault(x => x.Connection.Id == connectionId);

            if (subscriber is not null)
                subscriber.InFlight--;
        }
    }
}