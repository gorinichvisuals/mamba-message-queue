namespace MambaMQ.Server.QueueManagers;

internal sealed class QueueManager(IQueueStorageService queueStorageService, IQueueLogService queueLogService) : IQueueManager
{    
    private readonly Dictionary<Guid, MambaQueue> _queues = [];
    private readonly Dictionary<string, Guid> _queueNames = [];

    public async Task CreateQueue(string queueName, 
        bool isDurable,
        bool messageRetentionEnabled,
        TimeSpan messageRetentionPeriod,
        bool logsRetentionEnabled,
        LogLevel logRetentionLevel, 
        TimeSpan logsRetentionPeriod)
    {
        ValidateQueueOptions(isDurable, messageRetentionEnabled);

        MambaQueue? queue = GetQueue(queueName);

        if (queue is not null)
        {
            if(logsRetentionEnabled && (queue.LogLevel & LogLevel.Info) is not 0) 
                await queueLogService.Log(queue, LogLevel.Info, LogEventType.QueueAlreadyExists, $"Queue '{queueName}' already exists.");     
            
            return;
        }
        
        MambaQueue newQueue = new(
            Guid.CreateVersion7(),
            queueName,
            isDurable,
            messageRetentionEnabled,
            messageRetentionPeriod,
            logsRetentionEnabled,
            logRetentionLevel,
            logsRetentionPeriod);
        
        if (isDurable)
        {
            StoredMambaQueue storedQueue = new(
                newQueue.Id,
                newQueue.Name,
                newQueue.IsDurable,
                new StoredMessageRetentionOptions(
                    newQueue.MessageRetentionEnabled,
                    newQueue.MessageRetentionPeriod),
                new StoredLogRetentionOptions(
                    newQueue.LogRetentionEnabled,
                    newQueue.LogLevel,
                    newQueue.LogRetentionPeriod));

            await queueStorageService.SaveQueue(storedQueue);
        }
        
        _queues.Add(newQueue.Id, newQueue);
        _queueNames.Add(newQueue.Name, newQueue.Id);
        
        if(logsRetentionEnabled && (newQueue.LogLevel & LogLevel.Info) is not 0)
            await queueLogService.Log(newQueue, LogLevel.Info, LogEventType.QueueCreated, $"Queue '{newQueue.Name}' was created.");
    }

    public async Task PublishMessage(
        string queueName, 
        MambaMessage message, 
        CancellationToken cancellationToken = default)
    {
        MambaQueue? queue = GetQueue(queueName);
        
        if(queue is null)
            throw new InvalidOperationException($"Queue '{queueName}' does not exist.");
        
        if (queue.MessageRetentionEnabled)
            await PersistMessageIfRequired(queue, message, cancellationToken);
        
        queue.PublishMessage(message);
        
        if(queue.LogRetentionEnabled && (queue.LogLevel & LogLevel.Info) is not 0)
            await queueLogService.Log(queue, LogLevel.Info, LogEventType.MessagePublished, $"Message '{message.MessageId}' was published.", cancellationToken);
    }

    public Task SubscribeQueue(
        string queueName, 
        IClientConnection connection, 
        CancellationToken cancellationToken = default)
    {
        MambaQueue? queue = GetQueue(queueName);
        
        if(queue is null)
            throw new InvalidOperationException($"Queue '{queueName}' does not exist.");

        _ = Consume(queue, connection, cancellationToken);
        
        return Task.CompletedTask;
    }

    public async Task DeleteMessage(
        string queueName,
        Guid messageId, 
        CancellationToken cancellationToken = default)
    {
        Guid queueId = _queueNames[queueName];
        MambaQueue queue = _queues[queueId];
        
        if (queue.MessageRetentionEnabled)
            await queueStorageService.MarkAsDeleteMessage(queue.Id, messageId, cancellationToken);

        queue.DeleteMessage(messageId);
        
        if(queue.LogRetentionEnabled && (queue.LogLevel & LogLevel.Info) is not 0)
            await queueLogService.Log(queue, LogLevel.Info, LogEventType.MessageDeleted, $"Message '{messageId}' was deleted.", cancellationToken);
    }

    public async Task RestoreQueues(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<StoredMambaQueueState> storedQueues = await queueStorageService.RestoreQueues(cancellationToken);

        foreach (StoredMambaQueueState storedQueue in storedQueues)
        {
            cancellationToken.ThrowIfCancellationRequested();

            MambaQueue queue = new(
                storedQueue.Queue.Id, 
                storedQueue.Queue.Name, 
                storedQueue.Queue.IsDurable, 
                storedQueue.Queue.MessageRetention.RetentionEnabled,
                storedQueue.Queue.MessageRetention.RetentionPeriod,
                storedQueue.Queue.LogRetention.RetentionEnabled,
                storedQueue.Queue.LogRetention.LogLevel,
                storedQueue.Queue.LogRetention.RetentionPeriod);

            foreach (StoredMambaMessage message in storedQueue.Messages)
                queue.PublishMessage(new MambaMessage(message.Id, message.ReceivedAt, message.Body));
            
            _queues.Add(queue.Id, queue);
            _queueNames.Add(queue.Name, queue.Id);

            if (!queue.LogRetentionEnabled && (queue.LogLevel & LogLevel.Info) is 0) 
                continue;
            
            await queueLogService.Log(queue, LogLevel.Info, LogEventType.MessagesRestored, $"Restored {storedQueue.Messages.Count} messages.", cancellationToken);
            await queueLogService.Log(queue, LogLevel.Info, LogEventType.QueueRestored, $"Queue '{queue.Name}' was successfully restored.", cancellationToken);
        }
    }
    
    private async Task Consume(
        MambaQueue queue, 
        IClientConnection connection, 
        CancellationToken cancellationToken)
    {
        if(queue.LogRetentionEnabled && (queue.LogLevel & LogLevel.Info) is not 0)
            await queueLogService.Log(queue, LogLevel.Info, LogEventType.ConsumerConnected, $"Consumer '{connection.Id}' subscribed to queue '{queue.Name}'.", cancellationToken);
        
        try
        {
            await foreach (MessageDelivery delivery in queue.SubscribeAsync(connection.Id, cancellationToken))
            {
                byte[] payload = MessageEncoder.Encode(delivery.Message);

                Frame frame = new(FrameType.GetMessage, payload);

                await connection.SendAsync(frame, cancellationToken);
                
                if(queue.LogRetentionEnabled && (queue.LogLevel & LogLevel.Info) is not 0)
                    await queueLogService.Log(queue, LogLevel.Info, LogEventType.MessageDelivered, $"Message '{delivery.Message.MessageId}' was delivered to connection '{connection.Id}'.", cancellationToken);
            }
        }
        finally
        {
            if(queue.LogRetentionEnabled && (queue.LogLevel & LogLevel.Info) is not 0)
                await queueLogService.Log(queue, LogLevel.Info, LogEventType.ConsumerDisconnected, $"Consumer '{connection.Id}' unsubscribed from queue '{queue.Name}'.", CancellationToken.None);
        }
    }
    
    private async Task PersistMessageIfRequired(
        MambaQueue queue, 
        MambaMessage message, 
        CancellationToken cancellationToken)
    {
        StoredMambaMessage storedMessage = new(message.MessageId, message.ReceivedAt, message.Body);

        await queueStorageService.SaveMessage(queue.Id, storedMessage, cancellationToken);
        
        if(queue.LogRetentionEnabled && (queue.LogLevel & LogLevel.Info) is not 0)
            await queueLogService.Log(queue, LogLevel.Info, LogEventType.MessageSavedToStorage, $"Message '{message.MessageId}' was saved to storage.", cancellationToken);
    }
    
    private static void ValidateQueueOptions(bool isDurable, bool persistMessages)
    {
        if (!isDurable && persistMessages)
            throw new InvalidOperationException("PersistMessages cannot be enabled for a non-durable queue.");
    }
    
    private MambaQueue? GetQueue(string queueName)
        => !_queueNames.TryGetValue(queueName, out Guid queueId) 
            ? null 
            : _queues[queueId];
}