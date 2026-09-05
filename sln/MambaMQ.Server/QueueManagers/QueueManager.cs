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

    public Task SubscribeQueueAsync(string queueName, bool autoAcknowledge, IClientConnection connection, CancellationToken cancellationToken = default)
    {
        MambaQueue queue = GetOrCreateQueue(queueName);

        return ConsumeAsync(queue, autoAcknowledge, connection, cancellationToken);
    }

    public Task DeleteMessageAsync(string queueName, Guid messageId, CancellationToken cancellationToken = default)
    {
        QueueId queueId = _queueNames[queueName];
        MambaQueue queue = _queues[queueId];

        queue.DeleteMessage(messageId);

        return Task.CompletedTask;
    }
    
    private static async Task ConsumeAsync(MambaQueue queue, bool autoAcknowledge, IClientConnection connection, CancellationToken cancellationToken)
    {
        await foreach (MessageDelivery delivery in queue.SubscribeAsync(autoAcknowledge, connection.Id, cancellationToken))
        {
            byte[] payload = MessageEncoder.Encode(delivery.Message);

            Frame frame = new(FrameType.GetMessage, payload);

            await connection.SendAsync(frame, cancellationToken);
        }
    }
    
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