namespace MambaMQ.Core.Services;

internal sealed partial class QueueManager
{
    private async Task Consume(
        MambaQueue queue, 
        IClientConnection connection, 
        CancellationToken cancellationToken)
    {
        mambaLogger.Queue(queue.Name).LogInformation("Subscriber '{ConnectionId}' subscribed to queue.", connection.Id);
        
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
        
        mambaLogger.Queue(queue.Name).LogInformation("Subscriber '{ConnectionId}' subscribed to queue with batch.", connection.Id);

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
            mambaLogger.Queue(queue.Name).LogError(exception,"MessageId - {MessageMessageId}", message.MessageId);

            throw;
        }
    }
    
    private static void ValidateQueueOptions(bool isDurable, bool nessageRetentionEnabled)
    {
        if (!isDurable && nessageRetentionEnabled)
            throw new InvalidOperationException("PersistMessages cannot be enabled for a non-durable queue.");
    }
    
    private MambaQueue GetRequiredQueue(string queueName)
    {
        MambaQueue? queue = GetQueue(queueName);

        if (queue is not null) 
            return queue;
        
        _serverLogger.LogWarning("Queue {QueueName} does not exist.", queueName);

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

            Subscriber? subscriber = subscribers
                .FirstOrDefault(x => x.Connection.Id == connectionId);

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
    
    private MambaQueue? GetQueue(string queueName)
        => !_queueNames.TryGetValue(queueName, out Guid queueId) 
            ? null 
            : _queues[queueId];
}