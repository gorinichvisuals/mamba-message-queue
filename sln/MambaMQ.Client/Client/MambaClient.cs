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

    public async Task PublishAsync<T>(string queueName, T message, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);
        
        byte[] body = JsonSerializer.SerializeToUtf8Bytes(message);

        MambaMessage mambaMessage = new(body);

        PublishMessageCommand command = new(queueName, mambaMessage);

        await SendCommandAsync(command, cancellationToken);
    }

    public async IAsyncEnumerable<MambaMessage> SubscribeAsync(string queueName, bool autoAcknowledge = true, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);
        
        SubscribeQueueCommand command = new(queueName, autoAcknowledge);

        await SendCommandAsync(command, cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            Frame frame = await FrameReader.ReadAsync(_connection, _options.MaxMessageSizeInKilobytes, cancellationToken);

            MambaMessage message = MessageDecoder.Decode(frame.Payload.Span);

            yield return message;
        }
    }

    public async Task DeleteMessageAsync(string queueName, Guid messageId, CancellationToken cancellationToken = default)
    {
        DeleteMessageCommand command = new(queueName, messageId);

        await SendCommandAsync(command, cancellationToken);
    }
    
    private Task EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        return _connectTask ??= _connection.ConnectAsync(_options.Host, _options.Port, cancellationToken);
    }
    
    private Task SendCommandAsync(ICommand command, CancellationToken cancellationToken)
    {
        byte[] payload = CommandEncoder.Encode(command);

        Frame frame = new(command.Type, payload);

        byte[] buffer = FrameEncoder.Encode(frame);

        return _connection.SendAsync(buffer, cancellationToken);
    }

    public ValueTask DisposeAsync() => _connection.DisposeAsync();
}