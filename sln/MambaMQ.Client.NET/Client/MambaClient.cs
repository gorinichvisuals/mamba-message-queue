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

    public async Task<QueueModel> CreateQueueAsync(QueueOptions queueOptions, CancellationToken cancellationToken = default)
    {
        ValidateQueueOptions(queueOptions);

        await EnsureConnectedAsync(cancellationToken);

        QueueCreateCommand command = new(
            queueOptions.QueueName,
            queueOptions.IsDurable,
            queueOptions.LoadBalancingAlgorithm,
            queueOptions.Permissions,
            queueOptions.MessageRetention.Enabled,
            queueOptions.MessageRetention.RetentionPeriod);

        return await SendCommandAsync(command, QueueModelDecoder.Decode, cancellationToken);
    }

    public async Task<IReadOnlyList<QueueModel>> GetQueuesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);

        QueueGetListCommand listCommand = new();

        Frame responseFrame = await SendCommandAsync(listCommand, cancellationToken, FrameType.QueueGetListResponse);

        return responseFrame.Type is not FrameType.QueueGetListResponse
            ? throw new InvalidDataException($"Expected {FrameType.QueueGetListResponse} frame, but received {responseFrame.Type}.")
            : GetQueuesResponseDecoder.Decode(responseFrame.Payload.Span).Queues;
    }

    public async Task<QueueModel> UpdateQueueAsync(Guid queueId, QueueOptions queueOptions, CancellationToken cancellationToken = default)
    {
        ValidateQueueOptions(queueOptions);

        await EnsureConnectedAsync(cancellationToken);

        QueueUpdateCommand command = new(
            queueId,
            queueOptions.QueueName,
            queueOptions.IsDurable,
            queueOptions.LoadBalancingAlgorithm,
            queueOptions.Permissions,
            queueOptions.MessageRetention.Enabled,
            queueOptions.MessageRetention.RetentionPeriod);

        return await SendCommandAsync(command, QueueModelDecoder.Decode, cancellationToken);
    }

    public async Task DeleteQueueAsync(Guid queueId, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);

        QueueDeleteCommand command = new(queueId);

        await SendCommandAsync(command, cancellationToken);
    }

    public async Task PublishToQueueAsync<T>(string queueName, T message, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);

        byte[] body = JsonSerializer.SerializeToUtf8Bytes(message);

        MambaMessage mambaMessage = new(body);

        QueueMessagePublishCommand command = new(queueName, mambaMessage);

        await SendCommandAsync(command, cancellationToken);
    }

    public async IAsyncEnumerable<MambaMessage> SubscribeQueueAsync(string queueName, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);

        QueueSubscribeCommand command = new(queueName);

        await SendCommandAsync(command, cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            Frame frame = await FrameReader.ReadAsync(_connection, _options.MaxMessageSizeInBytes, cancellationToken);

            if (frame.Type is not FrameType.QueueMessageDeliverResponse)
                throw new InvalidDataException($"Expected {FrameType.QueueMessageDeliverResponse} frame, but received {frame.Type}.");

            MambaMessage message = MessageDecoder.Decode(frame.Payload.Span);

            yield return message;
        }
    }

    public async IAsyncEnumerable<IReadOnlyList<MambaMessage>> SubscribeQueueWithBatchAsync(
        string queueName, BatchSubscribeOptions options, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);

        QueueSubscribeBatchCommand command = new(
            queueName,
            options.MaxMessages,
            options.MaxBytes,
            options.MaxWaitTime,
            options.Weight);

        await SendCommandAsync(command, cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            Frame frame = await FrameReader.ReadAsync(_connection, _options.MaxMessageSizeInBytes, cancellationToken);

            if (frame.Type is not FrameType.QueueMessagesBatchResponse)
            {
                throw new InvalidDataException($"Expected {FrameType.QueueMessagesBatchResponse} frame, but received {frame.Type}.");
            }

            IReadOnlyList<MambaMessage> messages = MessageBatchDecoder.Decode(frame.Payload.Span);

            yield return messages;
        }
    }

    public async Task DeleteMessageAsync(string queueName, Guid messageId, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);

        QueueMessageDeleteCommand command = new(queueName, messageId);

        await SendCommandAsync(command, cancellationToken);
    }

    public async Task<GetMessagesResponse> GetMessagesAsync(string queueName, int skip, int take, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);

        QueueMessagesGetCommand command = new(queueName, skip, take);

        Frame responseFrame = await SendCommandAsync(command, cancellationToken, FrameType.QueueMessagesGetResponse);

        return GetMessagesResponseDecoder.Decode(responseFrame.Payload.Span);
    }

    public async Task DeleteMessagesAsync(string queueName, ICollection<Guid> messageIds, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);

        QueueMessagesDeleteCommand command = new(queueName, messageIds);

        await SendCommandAsync(command, cancellationToken);
    }

    public async Task<ExchangeModel> CreateExchangeAsync(ExchangeOptions exchangeOptions, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);

        ExchangeCreateCommand command = new(exchangeOptions.Name, exchangeOptions.IsDurable, exchangeOptions.Type, exchangeOptions.Permissions);

        return await SendCommandAsync(command, DecodeExchangeModel, cancellationToken);
    }

    public async Task<IReadOnlyList<ExchangeModel>> GetExchangesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);

        ExchangeGetListCommand listCommand = new();

        Frame responseFrame = await SendCommandAsync(listCommand, cancellationToken, FrameType.ExchangeGetListResponse);

        return responseFrame.Type is not FrameType.ExchangeGetListResponse
            ? throw new InvalidDataException($"Expected {FrameType.ExchangeGetListResponse} frame, but received {responseFrame.Type}.")
            : GetExchangesResponseDecoder.Decode(responseFrame.Payload.Span).Exchanges;
    }

    public async Task<ExchangeModel> UpdateExchangeAsync(Guid exchangeId, ExchangeOptions exchangeOptions, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);

        ExchangeUpdateCommand command = new(
            exchangeId,
            exchangeOptions.Name,
            exchangeOptions.IsDurable,
            exchangeOptions.Type,
            exchangeOptions.Permissions);

        return await SendCommandAsync(command, DecodeExchangeModel, cancellationToken);
    }

    public async Task DeleteExchangeAsync(Guid exchangeId, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);

        ExchangeDeleteCommand command = new(exchangeId);

        await SendCommandAsync(command, cancellationToken);
    }

    public async Task BindQueueToExchangeAsync(string exchangeName, string queueName, string routingKey, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);

        ExchangeBindCommand command = new(exchangeName, queueName, routingKey);

        await SendCommandAsync(command, cancellationToken);
    }

    public async Task UnbindQueueFromExchangeAsync(string exchangeName, string queueName, string routingKey, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);

        ExchangeUnbindCommand command = new(exchangeName, queueName, routingKey);

        await SendCommandAsync(command, cancellationToken);
    }

    public async Task PublishToExchangeAsync<T>(string exchangeName, string routingKey, T message, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);

        byte[] body = JsonSerializer.SerializeToUtf8Bytes(message);

        MambaMessage mambaMessage = new(body);

        ExchangeMessagePublishCommand command = new(exchangeName, routingKey, mambaMessage);

        await SendCommandAsync(command, cancellationToken);
    }

    private Task EnsureConnectedAsync(CancellationToken cancellationToken)
        => _connectTask ??= _connection.ConnectAsync(_options.Host, _options.Port, cancellationToken);

    private async Task<Frame> SendCommandAsync(ICommand command, CancellationToken cancellationToken, FrameType expectedResponseType = FrameType.CommandResultResponse)
    {
        byte[] payload = CommandEncoder.Encode(command);

        Frame frame = new(command.Type, payload);

        byte[] buffer = FrameEncoder.Encode(frame);

        await _connection.SendAsync(buffer, cancellationToken);

        Frame responseFrame = await FrameReader.ReadAsync(_connection, _options.MaxMessageSizeInBytes, cancellationToken);

        if (responseFrame.Type == expectedResponseType && expectedResponseType is not FrameType.CommandResultResponse)
            return responseFrame;

        if (responseFrame.Type is not FrameType.CommandResultResponse)
            throw new InvalidDataException($"Expected {expectedResponseType} or {FrameType.CommandResultResponse} frame, but received {responseFrame.Type}.");

        CommandResponse response = CommandResponseDecoder.Decode(responseFrame.Payload.Span);

        if (!response.IsSucceed)
            throw new CommandException(response.ErrorCode, response.ErrorMessage ?? "Command failed.");

        return expectedResponseType is FrameType.CommandResultResponse
            ? responseFrame
            : throw new InvalidDataException($"Expected {expectedResponseType} frame, but received {responseFrame.Type}.");
    }

    private async Task<T> SendCommandAsync<T>(ICommand command, Func<ReadOnlySpan<byte>, T> dataDecoder, CancellationToken cancellationToken)
    {
        byte[] payload = CommandEncoder.Encode(command);

        Frame frame = new(command.Type, payload);

        byte[] buffer = FrameEncoder.Encode(frame);

        await _connection.SendAsync(buffer, cancellationToken);

        Frame responseFrame = await FrameReader.ReadAsync(_connection, _options.MaxMessageSizeInBytes, cancellationToken);

        if (responseFrame.Type is not FrameType.CommandResultResponse)
            throw new InvalidDataException($"Expected {FrameType.CommandResultResponse} frame, but received {responseFrame.Type}.");

        CommandResponse<T> response = CommandResponseDecoder.Decode(responseFrame.Payload.Span, dataDecoder);

        if (!response.IsSucceed)
            throw new CommandException(response.ErrorCode, response.ErrorMessage ?? "Command failed.");

        return response.Data
            ?? throw new InvalidDataException("Command response did not contain data.");
    }

    private static ExchangeModel DecodeExchangeModel(ReadOnlySpan<byte> buffer)
    {
        int offset = 0;

        ExchangeModel exchange = ExchangeModelDecoder.Decode(buffer, ref offset);

        return offset == buffer.Length
            ? exchange
            : throw new InvalidDataException("Exchange response contains unexpected data.");
    }

    private static void ValidateQueueOptions(QueueOptions queueOptions)
    {
        if (queueOptions is { IsDurable: false, MessageRetention.Enabled: true })
            throw new ArgumentException("Message retention cannot be enabled for a non-durable queue.", nameof(queueOptions));
    }

    public ValueTask DisposeAsync()
        => _connection.DisposeAsync();
}
