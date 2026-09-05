namespace MambaMQ.Server.Queues;

public sealed class MambaQueue(string queueName)
{
    private static int _nextId = 1;
    public QueueId Id { get; } = new QueueId(Interlocked.Increment(ref _nextId));
    public string Name { get; } = queueName;

    private readonly Dictionary<Guid, MambaMessage> _messages = [];
    private readonly Queue<Guid> _available = [];
    private readonly Dictionary<DeliveryId, Delivery> _inFlight = [];
    
    private readonly SemaphoreSlim _messageAvailable = new(0);
    
    public void PublishMessage(MambaMessage message)
    {
        _messages.Add(message.MessageId, message);
        _available.Enqueue(message.MessageId);
        
        _messageAvailable.Release();
    }
    
    public async IAsyncEnumerable<MessageDelivery> SubscribeAsync(bool autoAcknowledge, Guid connectionId, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await _messageAvailable.WaitAsync(cancellationToken);

            Guid messageId = _available.Dequeue();

            MambaMessage message = _messages[messageId];

            DeliveryId deliveryId = new(Guid.CreateVersion7());

            Delivery delivery = new(deliveryId, message.MessageId, connectionId);

            if (autoAcknowledge)
                DeleteMessage(messageId);
            else
                _inFlight.Add(deliveryId, delivery);

            yield return new MessageDelivery(message, deliveryId);
        }
    }

    public void DeleteMessage(Guid messageId)
    {
        bool removed = _messages.Remove(messageId);

        if (!removed) return;
        
        RemoveFromAvailable(messageId);
        RemoveFromInFlight(messageId);
    }
    
    private void RemoveFromAvailable(Guid messageId)
    {
        if (_available.Count is 0)
            return;

        Guid[] remaining = _available
            .Where(id => id != messageId)
            .ToArray();
        
        _available.Clear();
        
        foreach (Guid id in remaining)
            _available.Enqueue(id);
    }
    
    private void RemoveFromInFlight(Guid messageId)
    {
        DeliveryId[] deliveries = _inFlight
            .Where(x => x.Value.MessageId == messageId)
            .Select(x => x.Key)
            .ToArray();

        foreach (DeliveryId deliveryId in deliveries)
            _inFlight.Remove(deliveryId);
    }
}