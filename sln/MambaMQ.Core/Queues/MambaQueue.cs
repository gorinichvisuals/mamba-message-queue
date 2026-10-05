namespace MambaMQ.Core.Queues;

public sealed class MambaQueue(
    Guid queueId,
    string queueName, 
    bool isDurable,
    DateTimeOffset createdAt,
    LoadBalancingAlgorithm loadBalancingAlgorithm,
    Dictionary<string, QueuePermission> permissions,
    bool messageRetentionEnabled, 
    TimeSpan messageRetentionPeriod)
{
    private long _nextDeliveryId = 1;
    
    public Guid Id { get; private set; } = queueId;
    public string Name { get; private set; } = queueName;
    public bool IsDurable { get; private set; } = isDurable;
    public LoadBalancingAlgorithm LoadBalancingAlgorithm { get; private set; } = loadBalancingAlgorithm;
    public bool MessageRetentionEnabled { get; private set; } = messageRetentionEnabled;
    public TimeSpan MessageRetentionPeriod { get; private set; } = messageRetentionPeriod;
    public Dictionary<string, QueuePermission> Permissions { get; private set; } = permissions;
    
    public DateTimeOffset CreatedAt { get; } = createdAt;
    
    public int AvailableMessageCount => _available.Count;
    public int InFlightMessageCount => _inFlight.Count;
    
    private readonly ConcurrentDictionary<Guid, MambaMessage> _messages = [];
    private readonly ConcurrentQueue<Guid> _available = [];
    
    private readonly ConcurrentDictionary<DeliveryId, Delivery> _inFlight = [];
    private readonly ConcurrentDictionary<Guid, DeliveryId> _deliveryByMessage = [];
    private readonly ConcurrentDictionary<Guid, int> _inFlightByConnection = [];
    
    private readonly SemaphoreSlim _messageAvailable = new(0);
    
    public void PublishMessage(MambaMessage message)
    {
        if(!_messages.TryAdd(message.MessageId, message))
            throw new InvalidOperationException($"Message '{message.MessageId}' already exists.");
        
        _available.Enqueue(message.MessageId);
        
        _messageAvailable.Release();
    }
    
    public async IAsyncEnumerable<MessageDelivery> SubscribeAsync(Guid connectionId, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await _messageAvailable.WaitAsync(cancellationToken);

            if (!_available.TryDequeue(out Guid messageId))
                continue;

            if (!_messages.TryGetValue(messageId, out MambaMessage? message))
                continue;

            yield return CreateDelivery(message, connectionId);
        }
    }
    
    public void DeleteMessage(Guid messageId)
    {
        _messages.TryRemove(messageId, out _);

        if (_deliveryByMessage.TryRemove(messageId, out DeliveryId deliveryId))
            _inFlight.TryRemove(deliveryId, out _);
    }
    
    public async ValueTask<MessageDelivery?> DequeueAsync(Guid connectionId, CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await _messageAvailable.WaitAsync(cancellationToken);

            if (!_available.TryDequeue(out Guid messageId))
                continue;

            if (!_messages.TryGetValue(messageId, out MambaMessage? message))
                continue;

            return CreateDelivery(message, connectionId);
        }

        return null;
    }
    
    public MambaMessage? PeekNextMessage()
    {
        while (_available.TryPeek(out Guid messageId))
        {
            if (_messages.TryGetValue(messageId, out MambaMessage? message))
                return message;

            _available.TryDequeue(out _);
        }

        return null;
    }
    
    private MessageDelivery CreateDelivery(MambaMessage message, Guid connectionId)
    {
        DeliveryId deliveryId = new(Interlocked.Increment(ref _nextDeliveryId));

        Delivery delivery = new(deliveryId, message.MessageId, connectionId);

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
    
    public void Update(
        string queueName,
        bool isDurable,
        LoadBalancingAlgorithm loadBalancingAlgorithm,
        Dictionary<string, QueuePermission> permissions,
        bool messageRetentionEnabled,
        TimeSpan messageRetentionPeriod)
    {
        Name = queueName;
        IsDurable = isDurable;
        LoadBalancingAlgorithm = loadBalancingAlgorithm;
        Permissions = permissions;
        MessageRetentionEnabled = messageRetentionEnabled;
        MessageRetentionPeriod = messageRetentionPeriod;
    }
    
    internal bool HasPermission(string serviceName, QueuePermission permission)
        => Permissions.TryGetValue(serviceName, out QueuePermission permissions) && permissions.HasFlag(permission);
}