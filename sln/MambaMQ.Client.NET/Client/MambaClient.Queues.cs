namespace MambaMQ.Client.NET;

internal sealed partial class MambaClient
{
    public async Task<QueueModel> CreateQueueAsync(QueueCreateOptions queueCreateOptions, CancellationToken cancellationToken = default)
    {
        ValidateQueueOptions(queueCreateOptions);

        await EnsureConnectedAsync(cancellationToken);

        QueueCreateCommand command = new(
            queueCreateOptions.QueueName,
            queueCreateOptions.IsDurable,
            queueCreateOptions.LoadBalancingAlgorithm,
            queueCreateOptions.Permissions,
            queueCreateOptions.MessageRetention.Enabled,
            queueCreateOptions.MessageRetention.RetentionPeriod);

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

    public async Task<QueueModel> UpdateQueueAsync(Guid queueId, QueueCreateOptions queueCreateOptions, CancellationToken cancellationToken = default)
    {
        ValidateId(queueId, nameof(queueId));
        ValidateQueueOptions(queueCreateOptions);

        await EnsureConnectedAsync(cancellationToken);

        QueueUpdateCommand command = new(
            queueId,
            queueCreateOptions.QueueName,
            queueCreateOptions.IsDurable,
            queueCreateOptions.LoadBalancingAlgorithm,
            queueCreateOptions.Permissions,
            queueCreateOptions.MessageRetention.Enabled,
            queueCreateOptions.MessageRetention.RetentionPeriod);

        return await SendCommandAsync(command, QueueModelDecoder.Decode, cancellationToken);
    }

    public async Task DeleteQueueAsync(Guid queueId, CancellationToken cancellationToken = default)
    {
        ValidateId(queueId, nameof(queueId));
        await EnsureConnectedAsync(cancellationToken);

        QueueDeleteCommand command = new(queueId);

        await SendCommandAsync(command, cancellationToken);
    }
    
    public async Task PublishToQueueAsync<T>(string queueName, T message, CancellationToken cancellationToken = default)
    {
        ValidateQueueName(queueName);
        await EnsureConnectedAsync(cancellationToken);

        byte[] body = JsonSerializer.SerializeToUtf8Bytes(message);

        MambaMessage mambaMessage = new(body);

        QueueMessagePublishCommand command = new(queueName, mambaMessage);

        await SendCommandAsync(command, cancellationToken);
    }

    public async IAsyncEnumerable<MambaMessage> SubscribeQueueAsync(string queueName, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ValidateQueueName(queueName);
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
        ValidateQueueName(queueName);
        ValidateBatchSubscribeOptions(options);
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
        ValidateQueueName(queueName);
        ValidateId(messageId, nameof(messageId));
        await EnsureConnectedAsync(cancellationToken);

        QueueMessageDeleteCommand command = new(queueName, messageId);

        await SendCommandAsync(command, cancellationToken);
    }

    public async Task<GetMessagesResponse> GetMessagesAsync(string queueName, int skip, int take, CancellationToken cancellationToken = default)
    {
        ValidateQueueName(queueName);
        if (skip < 0)
            throw new ArgumentOutOfRangeException(nameof(skip), "Skip cannot be negative.");
        if (take is < 1 or > 20)
            throw new ArgumentOutOfRangeException(nameof(take), "Page size must be between 1 and 20.");

        await EnsureConnectedAsync(cancellationToken);

        QueueMessagesGetCommand command = new(queueName, skip, take);

        Frame responseFrame = await SendCommandAsync(command, cancellationToken, FrameType.QueueMessagesGetResponse);

        return GetMessagesResponseDecoder.Decode(responseFrame.Payload.Span);
    }

    public async Task DeleteMessagesAsync(string queueName, ICollection<Guid> messageIds, CancellationToken cancellationToken = default)
    {
        ValidateQueueName(queueName);
        ArgumentNullException.ThrowIfNull(messageIds);
        if (messageIds.Count == 0)
            throw new ArgumentException("At least one message ID is required.", nameof(messageIds));
        if (messageIds.Any(static id => id == Guid.Empty))
            throw new ArgumentException("Message IDs cannot contain an empty GUID.", nameof(messageIds));

        await EnsureConnectedAsync(cancellationToken);

        QueueMessagesDeleteCommand command = new(queueName, messageIds);

        await SendCommandAsync(command, cancellationToken);
    }
}