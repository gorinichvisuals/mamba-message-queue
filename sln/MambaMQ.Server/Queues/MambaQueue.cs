namespace MambaMQ.Server.Queues;

public sealed class MambaQueue(string queueName)
{
    private static int _nextId = 1;
    private long _nextDeliveryId = 1;
    
    public QueueId Id { get; } = new QueueId(Interlocked.Increment(ref _nextId));
    public string Name { get; } = queueName;
    
    private readonly ConcurrentDictionary<Guid, MambaMessage> _messages = [];
    private readonly ConcurrentQueue<Guid> _available = [];
    
    private readonly ConcurrentDictionary<DeliveryId, Delivery> _inFlight = [];
    private readonly ConcurrentDictionary<Guid, DeliveryId> _deliveryByMessage = [];

    private readonly SemaphoreSlim _messageAvailable = new(0);
    
    public void PublishMessage(MambaMessage message)
    {
        if(!_messages.TryAdd(message.MessageId, message))
            throw new InvalidOperationException($"Message '{message.MessageId}' already exists.");
        
        _available.Enqueue(message.MessageId);
        
        _messageAvailable.Release();
    }
    
    public async IAsyncEnumerable<MessageDelivery> SubscribeAsync(
        bool autoAcknowledge, Guid connectionId, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await _messageAvailable.WaitAsync(cancellationToken);

            if (!_available.TryDequeue(out Guid messageId))
                continue;

            if (!_messages.TryGetValue(messageId, out MambaMessage? message))
                continue;

            yield return CreateDelivery(message, autoAcknowledge, connectionId);
        }
    }
    
    public void DeleteMessage(Guid messageId)
    {
        _messages.TryRemove(messageId, out _);

        if (_deliveryByMessage.TryRemove(messageId, out DeliveryId deliveryId))
            _inFlight.TryRemove(deliveryId, out _);
    }
    
    private MessageDelivery CreateDelivery(MambaMessage message, bool autoAcknowledge, Guid connectionId)
    {
        DeliveryId deliveryId = new(Interlocked.Increment(ref _nextDeliveryId));

        Delivery delivery = new(deliveryId, message.MessageId, connectionId);

        if (autoAcknowledge)
            DeleteMessage(message.MessageId);
        else
            TrackDelivery(delivery);

        return new MessageDelivery(message, deliveryId);
    }
    
    private void TrackDelivery(Delivery delivery)
    {
        if (!_inFlight.TryAdd(delivery.DeliveryId, delivery))
            throw new InvalidOperationException($"Delivery '{delivery.DeliveryId}' already exists.");

        if (_deliveryByMessage.TryAdd(delivery.MessageId, delivery.DeliveryId)) return;
            _inFlight.TryRemove(delivery.DeliveryId, out _);

        throw new InvalidOperationException($"Message '{delivery.MessageId}' is already in flight.");
    }
}