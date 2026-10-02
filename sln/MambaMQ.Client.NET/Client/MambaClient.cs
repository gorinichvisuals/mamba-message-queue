namespace MambaMQ.Client.NET;

internal sealed class MambaClient : IMamba, IAsyncDisposable
{
    private readonly IConnection _connection;
    private readonly MambaClientOptions _options;
    
    private Task? _connectTask;

    public MambaClient(
        IConnection connection,
        MambaClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(connection);

        _connection = connection;
        _options = options;
    }

    public async Task CreateQueueAsync(QueueOptions queueOptions, CancellationToken cancellationToken = default)
    {
        ValidateQueueOptions(queueOptions);
        
        await EnsureConnectedAsync(cancellationToken);
        
        CreateQueueCommand command = new(
            queueOptions.QueueName, 
            queueOptions.IsDurable, 
            queueOptions.LoadBalancingAlgorithm,
            queueOptions.Permissions,
            queueOptions.MessageRetention.Enabled,
            queueOptions.MessageRetention.RetentionPeriod);
        
        await SendCommandAsync(command, cancellationToken);
    }
    
    public async Task PublishAsync<T>(
        string queueName, 
        T message, 
        CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);
        
        byte[] body = JsonSerializer.SerializeToUtf8Bytes(message);

        MambaMessage mambaMessage = new(body);

        PublishMessageCommand command = new(queueName, mambaMessage);

        await SendCommandAsync(command, cancellationToken);
    }

    public async IAsyncEnumerable<MambaMessage> SubscribeQueueAsync(
        string queueName, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);
        
        SubscribeQueueCommand command = new(queueName);

        await SendCommandAsync(command, cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            Frame frame = await FrameReader.ReadAsync(_connection, _options.MaxMessageSizeInBytes, cancellationToken);

            MambaMessage message = MessageDecoder.Decode(frame.Payload.Span);

            yield return message;
        }
    }
    
    public async IAsyncEnumerable<IReadOnlyList<MambaMessage>> SubscribeWithBatchAsync(
        string queueName,
        BatchSubscribeOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);

        SubscribeQueueWithBatchCommand command = new(
            queueName,
            options.MaxMessages,
            options.MaxBytes,
            options.MaxWaitTime,
            options.Weight);

        await SendCommandAsync(command, cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            Frame frame = await FrameReader.ReadAsync(_connection, _options.MaxMessageSizeInBytes, cancellationToken);

            if (frame.Type is not FrameType.BatchMessages)
                throw new InvalidDataException($"Expected {FrameType.BatchMessages} frame, but received {frame.Type}.");

            IReadOnlyList<MambaMessage> messages = MessageBatchDecoder.Decode(frame.Payload.Span);

            yield return messages;
        }
    }

    public async Task DeleteMessageAsync(
        string queueName, 
        Guid messageId, 
        CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);
        
        DeleteMessageCommand command = new(queueName, messageId);

        await SendCommandAsync(command, cancellationToken);
    }
    
    private Task EnsureConnectedAsync(CancellationToken cancellationToken)
        => _connectTask ??= _connection.ConnectAsync(_options.Host, _options.Port, cancellationToken);
    
    private Task SendCommandAsync(ICommand command, CancellationToken cancellationToken)
    {
        byte[] payload = CommandEncoder.Encode(command);

        Frame frame = new(command.Type, payload);

        byte[] buffer = FrameEncoder.Encode(frame);

        return _connection.SendAsync(buffer, cancellationToken);
    }

    private static void ValidateQueueOptions(QueueOptions queueOptions)
    {
        if (queueOptions is { IsDurable: false, MessageRetention.Enabled: true })
            throw new ArgumentException("PersistMessages cannot be enabled for a non-durable queue.");
    }
    
    public ValueTask DisposeAsync() 
        => _connection.DisposeAsync();
}