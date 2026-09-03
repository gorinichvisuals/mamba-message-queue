namespace MambaMQ.Server.QueueManagers;

internal sealed class QueueManager : IQueueManager
{    
    private readonly Dictionary<QueueId, MambaQueue> _queues = [];
    private readonly Dictionary<string, QueueId> _queueNames = [];

    public Task PublishMessageAsync(string queueName, MambaMessage message, CancellationToken cancellationToken = default)
    {
        MambaQueue queue = GetOrCreateQueue(queueName);

        queue.PublishMessage(message);

        return Task.CompletedTask;
    }

    public IAsyncEnumerable<MessageDelivery> SubscribeQueueAsync(string queueName, bool autoAcknowledge, Guid connectionId, CancellationToken cancellationToken = default)
    {
        MambaQueue queue = GetOrCreateQueue(queueName);

        return queue.SubscribeAsync(autoAcknowledge, connectionId, cancellationToken);
    }

    public Task DeleteMessageAsync(string queueName, Guid messageId, CancellationToken cancellationToken = default)
    {
        QueueId queueId = _queueNames[queueName];
        MambaQueue queue = _queues[queueId];

        queue.DeleteMessage(messageId);

        return Task.CompletedTask;
    }
    
    public int RemoveExpiredMessages(DateTimeOffset now)
        => _queues.Values.Sum(queue => queue.RemoveExpiredMessages(now));
    
    private MambaQueue GetOrCreateQueue(string queueName)
    {
        if (_queueNames.TryGetValue(queueName, out QueueId queueId))
            return _queues[queueId];

        MambaQueue queue = new(queueName);

        _queues.Add(queue.Id, queue);
        _queueNames.Add(queue.Name, queue.Id);

        return queue;
    }
}