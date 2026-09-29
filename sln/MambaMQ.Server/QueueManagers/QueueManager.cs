namespace MambaMQ.Server.QueueManagers;

internal sealed class QueueManager(
    IServerStorageService serverStorageService, 
    IQueueLogger queueLogger,
    ILogger<QueueManager> logger) : IQueueManager
{    
    private readonly Dictionary<Guid, MambaQueue> _queues = [];
    private readonly Dictionary<string, Guid> _queueNames = [];
    private readonly Dictionary<Guid, List<Subscriber>> _subscribers = [];  
    private readonly Dictionary<Guid, ILoadBalancingStrategy> _loadBalancingStrategies = [];
    private readonly Dictionary<Guid, SemaphoreSlim> _batchLocks = [];
    
    public async Task CreateQueue(string queueName, 
        bool isDurable,
        LoadBalancingAlgorithm loadBalancingAlgorithm,
        bool messageRetentionEnabled,
        TimeSpan messageRetentionPeriod,
        bool logsRetentionEnabled,
        MambaServerLogLevel mambaServerLogRetentionLevel, 
        TimeSpan logsRetentionPeriod)
    {
        ValidateQueueOptions(isDurable, messageRetentionEnabled);

        MambaQueue? queue = GetQueue(queueName);

        if (queue is not null)
        {
            if(logsRetentionEnabled && (queue.MambaServerLogLevel & MambaServerLogLevel.Info) is not 0) 
                await queueLogger.Log(queue, MambaServerLogLevel.Info, LogEventType.QueueAlreadyExists, $"Queue '{queueName}' already exists.");     
            
            return;
        }
        
        MambaQueue newQueue = new(
            Guid.CreateVersion7(),
            queueName,
            isDurable,
            loadBalancingAlgorithm,
            messageRetentionEnabled,
            messageRetentionPeriod,
            logsRetentionEnabled,
            mambaServerLogRetentionLevel,
            logsRetentionPeriod);
        
        if (isDurable)
        {
            StoredMambaQueue storedQueue = new(
                newQueue.Id,
                newQueue.Name,
                newQueue.IsDurable,
                (byte)newQueue.LoadBalancingAlgorithm,
                new StoredMessageRetentionOptions(
                    newQueue.MessageRetentionEnabled,
                    newQueue.MessageRetentionPeriod),
                new StoredLogRetentionOptions(
                    newQueue.LogRetentionEnabled,
                    newQueue.MambaServerLogLevel,
                    newQueue.LogRetentionPeriod));

            try
            {
                await serverStorageService.SaveQueue(storedQueue);
            }
            catch (Exception exception)
            {
                if(logsRetentionEnabled && (newQueue.MambaServerLogLevel & MambaServerLogLevel.Errors) is not 0)
                    await queueLogger.Log(newQueue, MambaServerLogLevel.Errors, LogEventType.QueueNotCreated, $"{exception.Message}. Queue name - {newQueue.Name}");

                throw;
            }
        }
        
        _queues.Add(newQueue.Id, newQueue);
        _queueNames.Add(newQueue.Name, newQueue.Id);
        _loadBalancingStrategies.Add(newQueue.Id, CreateLoadBalancingStrategy(newQueue.LoadBalancingAlgorithm));
        _batchLocks.Add(newQueue.Id, new SemaphoreSlim(1, 1));
        
        if(logsRetentionEnabled && (newQueue.MambaServerLogLevel & MambaServerLogLevel.Info) is not 0)
            await queueLogger.Log(newQueue, MambaServerLogLevel.Info, LogEventType.QueueCreated, $"Queue '{newQueue.Name}' was created.");
    }

    public async Task PublishMessage(
        string queueName, 
        MambaMessage message, 
        CancellationToken cancellationToken = default)
    {
        MambaQueue queue = GetRequiredQueue(queueName);
        
        if (queue.MessageRetentionEnabled)
            await PersistMessageIfRequired(queue, message, cancellationToken);
        
        queue.PublishMessage(message);
        
        if(queue.LogRetentionEnabled && (queue.MambaServerLogLevel & MambaServerLogLevel.Info) is not 0)
            await queueLogger.Log(queue, MambaServerLogLevel.Info, LogEventType.MessagePublished, $"Message '{message.MessageId}' was published.", cancellationToken);
    }

    public Task SubscribeQueue(
        string queueName, 
        IClientConnection connection, 
        CancellationToken cancellationToken = default)
    {
        MambaQueue queue = GetRequiredQueue(queueName);

        _ = Consume(queue, connection, cancellationToken);
        
        return Task.CompletedTask;
    }

    public Task SubscribeQueueWithBatch(
        string queueName,
        IClientConnection connection,
        int maxMessages,
        int maxBytes,
        TimeSpan maxWaitTime,
        int weight,
        CancellationToken cancellationToken = default)
    {
        MambaQueue queue = GetRequiredQueue(queueName);

        Subscriber subscriber = new(
            connection,
            maxMessages,
            maxBytes,
            maxWaitTime,
            weight);

        lock (_subscribers)
        {
            if (!_subscribers.TryGetValue(queue.Id, out List<Subscriber>? subscribers))
            {
                subscribers = [];
                _subscribers.Add(queue.Id, subscribers);
            }

            subscribers.Add(subscriber);
        }

        _ = ConsumeWithBatch(queue, subscriber, cancellationToken);

        StartNextBatch(queue);
        
        logger.LogInformation("Subscriber {ConnectionId} is subscribed.", subscriber.Connection.Id);

        return Task.CompletedTask;
    }
    
    public async Task DeleteMessage(
        string queueName,
        Guid messageId,
        Guid connectionId,
        CancellationToken cancellationToken = default)
    {
        MambaQueue queue = GetRequiredQueue(queueName);
        
        try
        {
            await serverStorageService.MarkAsDeleteMessage(queue.Id, messageId, cancellationToken);
        }
        catch (Exception exception)
        {
            if(!queue.LogRetentionEnabled && (queue.MambaServerLogLevel & MambaServerLogLevel.Errors) is not 0)
                await queueLogger.Log(
                    queue, 
                    MambaServerLogLevel.Errors,
                    LogEventType.MessagesRestored, 
                    $"{exception.Message}. Queue name - {queue.Name}. MessageId - {messageId}", 
                    cancellationToken);

            throw;
        }

        queue.DeleteMessage(messageId);
        
        lock (_subscribers)
        {
            if (_subscribers.TryGetValue(queue.Id, out List<Subscriber>? subscribers))
            {
                Subscriber? subscriber = subscribers
                    .FirstOrDefault(x => x.Connection.Id == connectionId);

                if (subscriber is not null)
                    subscriber.InFlight--;
            }
        }
        
        if(queue.LogRetentionEnabled && (queue.MambaServerLogLevel & MambaServerLogLevel.Info) is not 0)
            await queueLogger.Log(queue, MambaServerLogLevel.Info, LogEventType.MessageDeleted, $"Message '{messageId}' was deleted.", cancellationToken);
    }

    public async Task RestoreQueues(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<StoredMambaQueueState> storedQueues = await serverStorageService.RestoreQueues(cancellationToken);

        logger.LogInformation("Successfully restored {QueueCount} queues.", storedQueues.Count);
        
        foreach (StoredMambaQueueState storedQueue in storedQueues)
        {
            cancellationToken.ThrowIfCancellationRequested();

            MambaQueue queue = new(
                storedQueue.Queue.Id, 
                storedQueue.Queue.Name,
                storedQueue.Queue.IsDurable, 
                (LoadBalancingAlgorithm)storedQueue.Queue.LoadBalancingAlgorithm,
                storedQueue.Queue.MessageRetention.RetentionEnabled,
                storedQueue.Queue.MessageRetention.RetentionPeriod,
                storedQueue.Queue.LogRetention.RetentionEnabled,
                storedQueue.Queue.LogRetention.MambaServerLogLevel,
                storedQueue.Queue.LogRetention.RetentionPeriod);

            foreach (StoredMambaMessage message in storedQueue.Messages)
                queue.PublishMessage(new MambaMessage(message.Id, message.ReceivedAt, message.Body));
            
            _queues.Add(queue.Id, queue);
            _queueNames.Add(queue.Name, queue.Id);
            _loadBalancingStrategies.Add(queue.Id, CreateLoadBalancingStrategy(queue.LoadBalancingAlgorithm));
            _batchLocks.Add(queue.Id, new SemaphoreSlim(1, 1));
            
            if (!queue.LogRetentionEnabled && (queue.MambaServerLogLevel & MambaServerLogLevel.Info) is 0) 
                continue;
            
            await queueLogger.Log(queue, MambaServerLogLevel.Info, LogEventType.MessagesRestored, $"Restored {storedQueue.Messages.Count} messages.", cancellationToken);
            await queueLogger.Log(queue, MambaServerLogLevel.Info, LogEventType.QueueRestored, $"Queue '{queue.Name}' was successfully restored.", cancellationToken);
        }
    }
    
    private static ILoadBalancingStrategy CreateLoadBalancingStrategy(LoadBalancingAlgorithm algorithm)
    {
        return algorithm switch
        {
            LoadBalancingAlgorithm.RoundRobin => new RoundRobinStrategy(),
            LoadBalancingAlgorithm.Random => new RandomStrategy(),
            LoadBalancingAlgorithm.WeightedRoundRobin => new WeightedRoundRobinStrategy(),
            LoadBalancingAlgorithm.LeastInFlight =>new LeastInFlightStrategy(),

            _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, null)
        };
    }
    
    private async Task Consume(
        MambaQueue queue, 
        IClientConnection connection, 
        CancellationToken cancellationToken)
    {
        if(queue.LogRetentionEnabled && (queue.MambaServerLogLevel & MambaServerLogLevel.Info) is not 0)
            await queueLogger.Log(queue, MambaServerLogLevel.Info, LogEventType.ConsumerConnected, $"Consumer '{connection.Id}' subscribed to queue '{queue.Name}'.", cancellationToken);
        
        try
        {
            await foreach (MessageDelivery delivery in queue.SubscribeAsync(connection.Id, cancellationToken))
            {
                byte[] payload = MessageEncoder.Encode(delivery.Message);

                Frame frame = new(FrameType.GetMessage, payload);

                await connection.SendAsync(frame, cancellationToken);
                
                if(queue.LogRetentionEnabled && (queue.MambaServerLogLevel & MambaServerLogLevel.Info) is not 0)
                    await queueLogger.Log(queue, MambaServerLogLevel.Info, LogEventType.MessageDelivered, $"Message '{delivery.Message.MessageId}' was delivered to connection '{connection.Id}'.", cancellationToken);
            }
        }
        finally
        {
            if(queue.LogRetentionEnabled && (queue.MambaServerLogLevel & MambaServerLogLevel.Info) is not 0)
                await queueLogger.Log(queue, MambaServerLogLevel.Info, LogEventType.ConsumerDisconnected, $"Consumer '{connection.Id}' unsubscribed from queue '{queue.Name}'.", CancellationToken.None);
        }
    }
    
    private async Task ConsumeWithBatch(
        MambaQueue queue,
        Subscriber subscriber,
        CancellationToken cancellationToken)
    {
        IClientConnection connection = subscriber.Connection;

        if (queue.LogRetentionEnabled &&
            (queue.MambaServerLogLevel & MambaServerLogLevel.Info) is not 0)
        {
            await queueLogger.Log(
                queue,
                MambaServerLogLevel.Info,
                LogEventType.ConsumerConnected,
                $"Consumer '{connection.Id}' subscribed to queue '{queue.Name}' with batch.",
                cancellationToken);
        }

        try
        {
            SemaphoreSlim batchLock = _batchLocks[queue.Id];

            while (!cancellationToken.IsCancellationRequested)
            {
                await subscriber.BatchSignal.WaitAsync(cancellationToken);

                logger.LogInformation("Subscriber {Id} woke up.", connection.Id);

                IReadOnlyList<MessageDelivery> deliveries;

                await batchLock.WaitAsync(cancellationToken);

                try
                {
                    deliveries = await DequeueBatch(queue, subscriber, cancellationToken);
                }
                finally
                {
                    batchLock.Release();
                }

                logger.LogInformation("Subscriber {Id} received batch size {Count}", connection.Id, deliveries.Count);

                if (deliveries.Count is 0)
                {
                    StartNextBatch(queue);
                    continue;
                }

                MambaMessage[] messages = new MambaMessage[deliveries.Count];

                for (int i = 0; i < deliveries.Count; i++)
                    messages[i] = deliveries[i].Message;

                byte[] payload = MessageBatchEncoder.Encode(messages);

                Frame frame = new(FrameType.BatchMessages, payload);

                await connection.SendAsync(frame, cancellationToken);

                subscriber.InFlight += deliveries.Count;

                if (queue.LogRetentionEnabled && (queue.MambaServerLogLevel & MambaServerLogLevel.Info) is not 0)
                {
                    foreach (MessageDelivery delivery in deliveries)
                    {
                        await queueLogger.Log(
                            queue,
                            MambaServerLogLevel.Info,
                            LogEventType.MessageDelivered,
                            $"Message '{delivery.Message.MessageId}' was delivered to connection '{connection.Id}'.",
                            cancellationToken);
                    }
                }

                StartNextBatch(queue);
            }
        }
        finally
        {
            lock (_subscribers)
            {
                if (_subscribers.TryGetValue(queue.Id, out List<Subscriber>? subscribers))
                {
                    subscribers.Remove(subscriber);

                    if (subscribers.Count is 0)
                        _subscribers.Remove(queue.Id);
                }
            }

            if (queue.LogRetentionEnabled &&
                (queue.MambaServerLogLevel & MambaServerLogLevel.Info) is not 0)
            {
                await queueLogger.Log(
                    queue,
                    MambaServerLogLevel.Info,
                    LogEventType.ConsumerDisconnected,
                    $"Consumer '{connection.Id}' unsubscribed from queue '{queue.Name}'.",
                    CancellationToken.None);
            }
        }
    }
    
    private async Task<IReadOnlyList<MessageDelivery>> DequeueBatch(
        MambaQueue queue,
        Subscriber subscriber,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("DequeueAsync waiting...");
        
        List<MessageDelivery> deliveries = [];

        int bytes = 0;

        using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        timeoutCts.CancelAfter(subscriber.MaxWaitTime);

        while (deliveries.Count < subscriber.MaxMessages)
        {
            MambaMessage? nextMessage = queue.PeekNextMessage();

            if (nextMessage is null)
                try
                {
                    MessageDelivery? delivery = await queue.DequeueAsync(subscriber.Connection.Id, timeoutCts.Token);

                    if (delivery is null)
                        break;

                    deliveries.Add(delivery);
                    bytes += delivery.Message.Body.Length;

                    continue;
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    break;
                }

            int messageSize = nextMessage.Body.Length;

            if (deliveries.Count > 0 && bytes + messageSize > subscriber.MaxBytes)
                break;

            MessageDelivery? nextDelivery = await queue.DequeueAsync(subscriber.Connection.Id, timeoutCts.Token);

            if (nextDelivery is null)
                break;

            deliveries.Add(nextDelivery);
            bytes += messageSize;

            if (bytes >= subscriber.MaxBytes)
                break;
        }

        return deliveries;
    }

    private async Task PersistMessageIfRequired(
        MambaQueue queue, 
        MambaMessage message, 
        CancellationToken cancellationToken)
    {
        StoredMambaMessage storedMessage = new(message.MessageId, message.ReceivedAt, message.Body);

        try
        {
            await serverStorageService.SaveMessage(queue.Id, storedMessage, cancellationToken);
        }
        catch (Exception exception)
        {
            if(queue.LogRetentionEnabled && (queue.MambaServerLogLevel & MambaServerLogLevel.Errors) is not 0)
                await queueLogger.Log(
                    queue, 
                    MambaServerLogLevel.Errors,
                    LogEventType.MessageNotSavedToStorage, 
                    $"{exception.Message}. Queue name - {queue.Name}. MessageId - {message.MessageId}", 
                    cancellationToken);

            throw;
        }
        
        if(queue.LogRetentionEnabled && (queue.MambaServerLogLevel & MambaServerLogLevel.Info) is not 0)
            await queueLogger.Log(queue, MambaServerLogLevel.Info, LogEventType.MessageSavedToStorage, $"Message '{message.MessageId}' was saved to storage.", cancellationToken);
    }
    
    private static void ValidateQueueOptions(bool isDurable, bool persistMessages)
    {
        if (!isDurable && persistMessages)
            throw new InvalidOperationException("PersistMessages cannot be enabled for a non-durable queue.");
    }
    
    private MambaQueue GetRequiredQueue(string queueName)
    {
        MambaQueue? queue = GetQueue(queueName);

        if (queue is not null) 
            return queue;
        
        logger.LogWarning("Queue {QueueName} does not exist.", queueName);

        throw new InvalidOperationException($"Queue '{queueName}' does not exist.");
    }
    
    private void StartNextBatch(MambaQueue queue)
    {
        Subscriber subscriber;

        lock (_subscribers)
        {
            if (!_subscribers.TryGetValue(queue.Id, out List<Subscriber>? subscribers) || subscribers.Count is 0)
                return;

            ILoadBalancingStrategy strategy = _loadBalancingStrategies[queue.Id];

            subscriber = strategy.Select(subscribers);
        }

        subscriber.BatchSignal.Release();
    }
    
    private MambaQueue? GetQueue(string queueName)
        => !_queueNames.TryGetValue(queueName, out Guid queueId) 
            ? null 
            : _queues[queueId];
}