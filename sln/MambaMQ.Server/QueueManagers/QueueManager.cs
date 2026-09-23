namespace MambaMQ.Server.QueueManagers;

internal sealed class QueueManager(IQueueStorageService queueStorageService) : IQueueManager
{    
    private readonly Dictionary<Guid, MambaQueue> _queues = [];
    private readonly Dictionary<string, Guid> _queueNames = [];

    public async Task PublishMessage(
        string queueName, 
        bool isDurable, 
        bool persistMessages, 
        MambaMessage message, 
        CancellationToken cancellationToken = default)
    {
        MambaQueue queue = await GetOrCreateQueue(queueName, isDurable, persistMessages);
        
        if (queue.PersistMessages)
            await PersistMessageIfRequired(queue, message, cancellationToken);
        
        queue.PublishMessage(message);
    }

    public async Task SubscribeQueue(
        string queueName, 
        bool isDurable, 
        bool persistMessages, 
        IClientConnection connection, 
        CancellationToken cancellationToken = default)
    {
        MambaQueue queue = await GetOrCreateQueue(queueName,  isDurable, persistMessages);

        _ = Consume(queue, connection, cancellationToken);
    }

    public async Task DeleteMessage(
        string queueName,
        Guid messageId, 
        CancellationToken cancellationToken = default)
    {
        Guid queueId = _queueNames[queueName];
        MambaQueue queue = _queues[queueId];
        
        if (queue.PersistMessages)
            await queueStorageService.DeleteMessage(queue.Id, messageId, cancellationToken);

        queue.DeleteMessage(messageId);
    }

    public async Task RestoreQueues(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<StoredMambaQueueState> storedQueues = await queueStorageService.RestoreQueues(cancellationToken);

        foreach (StoredMambaQueueState storedQueue in storedQueues)
        {
            cancellationToken.ThrowIfCancellationRequested();

            MambaQueue queue = new(storedQueue.Queue.Id, storedQueue.Queue.Name, storedQueue.Queue.IsDurable, storedQueue.Queue.PersistMessages);
            
            foreach (StoredMambaMessage message in storedQueue.Messages)
                queue.PublishMessage(new MambaMessage(message.Id, message.ReceivedAt, message.Body));

            _queues.Add(queue.Id, queue);
            _queueNames.Add(queue.Name, queue.Id);
        }
    }
    
    private async Task Consume(
        MambaQueue queue, 
        IClientConnection connection, 
        CancellationToken cancellationToken)
    {
        await foreach (MessageDelivery delivery in queue.SubscribeAsync(connection.Id, cancellationToken))
        {
            byte[] payload = MessageEncoder.Encode(delivery.Message);

            Frame frame = new(FrameType.GetMessage, payload);

            await connection.SendAsync(frame, cancellationToken);
        }
    }
    
    private async Task<MambaQueue> GetOrCreateQueue(
        string queueName, 
        bool isDurable, 
        bool persistMessages)
    {
        MambaQueue? queue = GetQueue(queueName);

        if (queue is null)
            return await CreateQueue(queueName, isDurable, persistMessages);

        if (queue.IsDurable != isDurable || queue.PersistMessages != persistMessages)
            throw new InvalidOperationException($"Queue '{queueName}' already exists with different settings.");

        return queue;
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
    
    private async Task<MambaQueue> CreateQueue(
        string queueName, 
        bool isDurable, 
        bool persistMessages)
    {
        ValidateQueueOptions(isDurable, persistMessages);
        
        MambaQueue queue = new(Guid.CreateVersion7(), queueName, isDurable, persistMessages);
        
        if (queue.IsDurable)
        {
            StoredMambaQueue storedQueue = new(queue.Id, queue.Name, queue.IsDurable, queue.PersistMessages);

            await queueStorageService.SaveQueue(storedQueue);
        }
        
        _queues.Add(queue.Id, queue);
        _queueNames.Add(queue.Name, queue.Id);

        return queue;
    }
    
    private async Task PersistMessageIfRequired(
        MambaQueue queue, 
        MambaMessage message, 
        CancellationToken cancellationToken)
    {
        StoredMambaMessage storedMessage = new(message.MessageId, message.ReceivedAt, message.Body);

        await queueStorageService.SaveMessage(queue.Id, storedMessage, cancellationToken);
    }
}