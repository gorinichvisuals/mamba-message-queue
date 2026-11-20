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
    private readonly PerSecondRateCounter _publishedMessageRate = new();
    private readonly PerSecondRateCounter _deliveredMessageRate = new();
    
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
    
    public long PublishedMessagesPerSecond => _publishedMessageRate.Value;
    public long DeliveredMessagesPerSecond => _deliveredMessageRate.Value;

    private readonly ConcurrentDictionary<Guid, MambaMessage> _messages = [];
    private readonly ConcurrentQueue<Guid> _available = [];
    
    private readonly ConcurrentDictionary<DeliveryId, Delivery> _inFlight = [];
    private readonly ConcurrentDictionary<Guid, DeliveryId> _deliveryByMessage = [];
    private readonly ConcurrentDictionary<Guid, int> _inFlightByConnection = [];
    
    private readonly SemaphoreSlim _messageAvailable = new(0);
    
    internal void PublishMessage(MambaMessage message, bool countForWriteRate = true)
    {
        if(!_messages.TryAdd(message.MessageId, message))
            throw new InvalidOperationException($"Message '{message.MessageId}' already exists.");
        
        _available.Enqueue(message.MessageId);
        
        _messageAvailable.Release();

        if (countForWriteRate)
            _publishedMessageRate.Add(1);
    }
    
    internal async IAsyncEnumerable<MessageDelivery> SubscribeAsync(Guid connectionId, [EnumeratorCancellation] CancellationToken cancellationToken = default)
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
    
    internal void DeleteMessage(Guid messageId)
    {
        _messages.TryRemove(messageId, out _);

        if (_deliveryByMessage.TryRemove(messageId, out DeliveryId deliveryId))
            _inFlight.TryRemove(deliveryId, out _);
    }
    
    internal void DeleteMessages(ICollection<Guid> messageIds)
    {
        HashSet<Guid> idsToDelete = messageIds.ToHashSet();

        foreach (Guid messageId in idsToDelete)
        {
            _messages.TryRemove(messageId, out _);

            if (_deliveryByMessage.TryRemove(messageId, out DeliveryId deliveryId))
                _inFlight.TryRemove(deliveryId, out _);
        }

        int availableCount = _available.Count;

        for (int i = 0; i < availableCount; i++)
        {
            if (!_available.TryDequeue(out Guid messageId))
                break;

            if (!idsToDelete.Contains(messageId))
                _available.Enqueue(messageId);
        }
    }

    internal void RequeueInFlight(Guid connectionId)
    {
        foreach ((DeliveryId deliveryId, Delivery delivery) in _inFlight)
        {
            if (delivery.ConsumerId != connectionId || !_inFlight.TryRemove(deliveryId, out _))
                continue;

            _deliveryByMessage.TryRemove(delivery.MessageId, out _);

            if (!_messages.ContainsKey(delivery.MessageId))
                continue;

            _available.Enqueue(delivery.MessageId);
            _messageAvailable.Release();
        }
    }
    
    internal void RecordDeliveredMessages(int count)
    {
        if (count > 0)
            _deliveredMessageRate.Add(count);
    }

    internal async ValueTask<MessageDelivery?> DequeueAsync(Guid connectionId, CancellationToken cancellationToken = default)
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
    
    internal MambaMessage? PeekNextMessage()
    {
        while (_available.TryPeek(out Guid messageId))
        {
            if (_messages.TryGetValue(messageId, out MambaMessage? message))
                return message;

            _available.TryDequeue(out _);
        }

        return null;
    }
    
    internal void Update(QueueUpdateDto dto)
    {
        Name = dto.QueueName;
        LoadBalancingAlgorithm = dto.LoadBalancingAlgorithm;
        Permissions = dto.Permissions;
        MessageRetentionPeriod = dto.MessageRetentionPeriod;
    }
    
    internal (IReadOnlyList<MambaMessage> Messages, int TotalCount) GetMessages(int skip, int take)
    {
        int totalCount = _messages.Count;

        IReadOnlyList<MambaMessage> messages = _messages.Values
            .OrderByDescending(message => message.ReceivedAt)
            .ThenByDescending(message => message.MessageId)
            .Skip(skip)
            .Take(take)
            .ToArray();

        return (messages, totalCount);
    }
    
    internal bool HasPermission(string serviceName, QueuePermission permission)
        => Permissions.TryGetValue(serviceName, out QueuePermission servicePermissions) && servicePermissions.HasFlag(permission);
    
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

    private sealed class PerSecondRateCounter
    {
        private readonly object _lock = new();
        private PublishRateBucket _currentBucket = new(GetCurrentSecond());
        private PublishRateBucket? _previousBucket;

        public long Value
        {
            get
            {
                lock (_lock)
                {
                    AdvanceTo(GetCurrentSecond());
                    return _previousBucket is null ? 0 : Interlocked.Read(ref _previousBucket.Count);
                }
            }
        }

        public void Add(long count)
        {
            long currentSecond = GetCurrentSecond();
            PublishRateBucket bucket = Volatile.Read(ref _currentBucket);

            if (bucket.Second == currentSecond)
            {
                Interlocked.Add(ref bucket.Count, count);
                return;
            }

            lock (_lock)
            {
                AdvanceTo(currentSecond);
                Interlocked.Add(ref _currentBucket.Count, count);
            }
        }

        private void AdvanceTo(long currentSecond)
        {
            PublishRateBucket currentBucket = _currentBucket;
            if (currentSecond <= currentBucket.Second)
                return;

            _previousBucket = currentSecond == currentBucket.Second + 1 ? currentBucket : null;
            Volatile.Write(ref _currentBucket, new PublishRateBucket(currentSecond));
        }

        private static long GetCurrentSecond()
            => System.Diagnostics.Stopwatch.GetTimestamp() / System.Diagnostics.Stopwatch.Frequency;

        private sealed class PublishRateBucket(long second)
        {
            public long Second { get; } = second;
            public long Count;
        }
    }
}