namespace MambaMQ.Core.Services.Implementations;

internal sealed partial class QueueManager
{
    private static QueueModel ToQueueModel(MambaQueue queue)
    {
        return new QueueModel
        {
            Id = queue.Id,
            Name = queue.Name,
            IsDurable = queue.IsDurable,
            CreatedAt = queue.CreatedAt,
            LoadBalancingAlgorithm = queue.LoadBalancingAlgorithm,
            Permissions = new Dictionary<string, QueuePermission>(
                queue.Permissions),
            MessageRetentionEnabled = queue.MessageRetentionEnabled,
            MessageRetentionPeriod = queue.MessageRetentionPeriod,
            AvailableMessageCount = queue.AvailableMessageCount,
            InFlightMessageCount = queue.InFlightMessageCount
        };
    }
    
    private async Task<CommandResponse> PersistQueue(MambaQueue queue, CancellationToken cancellationToken = default)
    {
        StoredMambaQueue storedQueue = new(
            queue.Id,
            queue.Name,
            queue.IsDurable,
            queue.CreatedAt,
            (byte)queue.LoadBalancingAlgorithm,
            queue.Permissions.ToDictionary(
                x => x.Key,
                x => (byte)x.Value),
            new StoredMessageRetentionOptions(
                queue.MessageRetentionEnabled,
                queue.MessageRetentionPeriod));

        try
        {
            await serverStorageService.SaveQueue(storedQueue, cancellationToken);

            return CommandResponse.Success();
        }
        catch (Exception exception)
        {
            mambaLogger.Queue(queue.Name).LogError(exception, "Failed to persist queue.");

            return CommandResponse.Fail(ErrorCode.PersistenceError, "Failed to persist queue.");
        }
    }
    
    private bool HasQueuePermission(MambaQueue queue, IClientConnection connection, QueuePermission permission)
    {
        if (connection.ClientType is ClientType.Management)
            return true;
        
        if (!authorizationEnabled)
            return true;

        return connection?.ServiceName is not null && queue.HasPermission(connection.ServiceName, permission);
    }
    
    private async Task Consume(
        MambaQueue queue,
        IClientConnection connection,
        CancellationToken cancellationToken)
    {
        mambaLogger.Queue(queue.Name).LogInformation("Subscriber '{ServiceName}' with ID '{ConnectionId}' subscribed to queue.", connection.ServiceName, connection.Id);

        try
        {
            await foreach (MessageDelivery delivery in queue.SubscribeAsync(connection.Id, cancellationToken))
            {
                byte[] payload = MessageEncoder.Encode(delivery.Message);

                Frame frame = new(FrameType.GetMessage, payload);

                await connection.SendAsync(frame, cancellationToken);

                mambaLogger.Queue(queue.Name).LogDebug("Message '{MessageMessageId}' was delivered to connection '{ConnectionId}'.", delivery.Message.MessageId, connection.Id);
            }
        }
        finally
        {
            mambaLogger.Queue(queue.Name).LogInformation("Subscriber '{ConnectionId}' unsubscribed from queue.", connection.Id);
        }
    }
    
    private async Task ConsumeWithBatch(
        MambaQueue queue,
        Subscriber subscriber,
        CancellationToken cancellationToken)
    {
        IClientConnection connection = subscriber.Connection;
        
        mambaLogger.Queue(queue.Name).LogInformation("Subscriber '{ServiceName}' with ID '{ConnectionId}' subscribed to queue with batch.", connection.ServiceName, connection.Id);

        try
        {
            SemaphoreSlim batchLock = _batchLocks[queue.Id];

            while (!cancellationToken.IsCancellationRequested)
            {
                await subscriber.BatchSignal.WaitAsync(cancellationToken);

                mambaLogger.Queue(queue.Name).LogDebug("Subscriber {Id} woke up.", connection.Id);

                IReadOnlyList<MessageDelivery> deliveries = await DequeueBatchWithLock(queue, subscriber, batchLock, cancellationToken);

                mambaLogger.Queue(queue.Name).LogDebug("Subscriber {Id} received batch size {Count}", connection.Id, deliveries.Count);

                if (deliveries.Count is 0)
                {
                    StartNextBatch(queue);
                    continue;
                }

                await SendBatch(queue, subscriber, deliveries, cancellationToken);

                StartNextBatch(queue);
            }
        }
        finally
        {
            RemoveSubscriber(queue.Id, subscriber);
            
            mambaLogger.Queue(queue.Name).LogInformation("Subscriber '{ConnectionId}' unsubscribed from queue.", connection.Id);
        }
    }
    
    private async Task<IReadOnlyList<MessageDelivery>> DequeueBatchWithLock(
        MambaQueue queue,
        Subscriber subscriber,
        SemaphoreSlim batchLock,
        CancellationToken cancellationToken)
    {
        await batchLock.WaitAsync(cancellationToken);

        try
        {
            return await DequeueBatch(queue, subscriber, cancellationToken);
        }
        finally
        {
            batchLock.Release();
        }
    }
    
    private async Task SendBatch(
        MambaQueue queue,
        Subscriber subscriber,
        IReadOnlyList<MessageDelivery> deliveries,
        CancellationToken cancellationToken)
    {
        MambaMessage[] messages = new MambaMessage[deliveries.Count];

        for (int i = 0; i < deliveries.Count; i++)
            messages[i] = deliveries[i].Message;

        byte[] payload = MessageBatchEncoder.Encode(messages);

        Frame frame = new(FrameType.BatchMessages, payload);

        await subscriber.Connection.SendAsync(frame, cancellationToken);

        subscriber.InFlight += deliveries.Count;

        foreach (MessageDelivery delivery in deliveries)
            mambaLogger.Queue(queue.Name).LogDebug("Message '{MessageMessageId}' was delivered to connection '{ConnectionId}'.", delivery.Message.MessageId, subscriber.Connection.Id);
    }
    
    private async Task<IReadOnlyList<MessageDelivery>> DequeueBatch(
        MambaQueue queue,
        Subscriber subscriber,
        CancellationToken cancellationToken)
    {
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
    
    private static CommandResponse ValidateQueueOptions(bool isDurable, bool messageRetentionEnabled)
    {
        if (!isDurable && messageRetentionEnabled)
            return CommandResponse.Fail(ErrorCode.InvalidArgument, "Message retention cannot be enabled for a non-durable queue.");

        return CommandResponse.Success();
    }
    
    private static MambaQueue RestoreQueue(StoredMambaQueueState storedQueue)
    {
        MambaQueue queue = new(
            storedQueue.Queue.Id,
            storedQueue.Queue.Name,
            storedQueue.Queue.IsDurable,
            storedQueue.Queue.CreatedAt,
            (LoadBalancingAlgorithm)storedQueue.Queue.LoadBalancingAlgorithm,
            storedQueue.Queue.Permissions.ToDictionary(
                x => x.Key,
                x => (QueuePermission)x.Value),
            storedQueue.Queue.MessageRetention.RetentionEnabled,
            storedQueue.Queue.MessageRetention.RetentionPeriod);

        foreach (StoredMambaMessage message in storedQueue.Messages)
            queue.PublishMessage(new MambaMessage(message.Id, message.ReceivedAt, message.Body));

        return queue;
    }
    
    private void RegisterQueue(MambaQueue queue)
    {
        _queues.Add(queue.Id, queue);
        _queueNames.Add(queue.Name, queue.Id);
        _loadBalancingStrategies.Add(queue.Id, LoadBalancingStrategyFactory.Create(queue.LoadBalancingAlgorithm));
        _batchLocks.Add(queue.Id, new SemaphoreSlim(1, 1));
    }
    
    private void StartNextBatch(MambaQueue queue)
    {
        Subscriber subscriber;

        lock (_subscribers)
        {
            if (!_subscribers.TryGetValue(queue.Id, out List<Subscriber>? subscribers) || subscribers.Count is 0)
                return;

            ILoadBalancingStrategy strategy = _loadBalancingStrategies[queue.Id];

            subscriber = strategy.SelectSubscriber(subscribers);
        }

        subscriber.BatchSignal.Release();
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
    
    private void AddSubscriber(Guid queueId, Subscriber subscriber)
    {
        lock (_subscribers)
        {
            if (!_subscribers.TryGetValue(queueId, out List<Subscriber>? subscribers))
            {
                subscribers = [];
                _subscribers.Add(queueId, subscribers);
            }

            subscribers.Add(subscriber);
        }
    }
    
    private void RemoveSubscriber(Guid queueId, Subscriber subscriber)
    {
        lock (_subscribers)
        {
            if (!_subscribers.TryGetValue(queueId, out List<Subscriber>? subscribers))
                return;

            subscribers.Remove(subscriber);

            if (subscribers.Count is 0)
                _subscribers.Remove(queueId);
        }
    }
    
    private MambaQueue? GetQueueById(Guid queueId)
        => _queues.GetValueOrDefault(queueId);
    
    private MambaQueue? GetQueueByName(string queueName)
        => !_queueNames.TryGetValue(queueName, out Guid queueId) 
            ? null 
            : _queues[queueId];
}