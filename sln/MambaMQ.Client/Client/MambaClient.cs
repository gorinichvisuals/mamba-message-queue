namespace MambaMQ.Client;

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
            queueOptions.MessageRetention.Enabled,
            queueOptions.MessageRetention.RetentionPeriod,
            queueOptions.LogRetention.Enabled,
            (byte)queueOptions.LogRetention.LogLevel,
            queueOptions.LogRetention.RetentionPeriod);
        
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

    public async IAsyncEnumerable<MambaMessage> SubscribeAsync(
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
        
        switch (queueOptions.LogRetention.Enabled)
        {
            case false when queueOptions.LogRetention.LogLevel is not QueueLogLevel.None:
                throw new InvalidOperationException("LogLevel must be None when log retention is disabled.");
            case true when queueOptions.LogRetention.LogLevel is QueueLogLevel.None:
                throw new InvalidOperationException("LogLevel cannot be None when log retention is enabled.");
        }
    }
    
    public ValueTask DisposeAsync() 
        => _connection.DisposeAsync();
}