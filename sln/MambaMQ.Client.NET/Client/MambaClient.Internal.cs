namespace MambaMQ.Client.NET;

internal sealed partial class MambaClient
{
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

    private static void ValidateQueueOptions(QueueCreateOptions queueCreateOptions)
    {
        ArgumentNullException.ThrowIfNull(queueCreateOptions);
        ValidateName(queueCreateOptions.QueueName, nameof(queueCreateOptions.QueueName));
        ArgumentNullException.ThrowIfNull(queueCreateOptions.MessageRetention);

        if (!Enum.IsDefined(queueCreateOptions.LoadBalancingAlgorithm))
            throw new ArgumentOutOfRangeException(nameof(queueCreateOptions.LoadBalancingAlgorithm), "Unknown load balancing algorithm.");

        ValidatePermissions(queueCreateOptions.Permissions, QueuePermission.Read | QueuePermission.Write | QueuePermission.Delete, nameof(queueCreateOptions.Permissions));

        if (queueCreateOptions.MessageRetention.Enabled && queueCreateOptions.MessageRetention.RetentionPeriod <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(queueCreateOptions.MessageRetention.RetentionPeriod), "Retention period must be greater than zero when retention is enabled.");

        if (!queueCreateOptions.IsDurable && queueCreateOptions.MessageRetention.Enabled)
            throw new ArgumentException("Message retention cannot be enabled for a non-durable queue.", nameof(queueCreateOptions));
    }

    private static void ValidateExchangeOptions(ExchangeCreateOptions exchangeCreateOptions)
    {
        ArgumentNullException.ThrowIfNull(exchangeCreateOptions);
        ValidateName(exchangeCreateOptions.Name, nameof(exchangeCreateOptions.Name));

        if (!Enum.IsDefined(exchangeCreateOptions.Type))
            throw new ArgumentOutOfRangeException(nameof(exchangeCreateOptions.Type), "Unknown exchange type.");

        ValidatePermissions(exchangeCreateOptions.Permissions, ExchangePermission.Read | ExchangePermission.Write | ExchangePermission.Delete, nameof(exchangeCreateOptions.Permissions));
    }

    private static void ValidateId(Guid id, string parameterName)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("ID cannot be an empty GUID.", parameterName);
    }

    private static void ValidateName(string name, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty or whitespace.", parameterName);
    }

    private static void ValidatePermissions<TPermission>(Dictionary<string, TPermission> permissions, TPermission allowedPermissions, string parameterName)
        where TPermission : struct, Enum
    {
        ArgumentNullException.ThrowIfNull(permissions, parameterName);

        foreach ((string serviceName, TPermission permission) in permissions)
        {
            ValidateName(serviceName, parameterName);

            if ((Convert.ToByte(permission) & ~Convert.ToByte(allowedPermissions)) is not 0)
                throw new ArgumentOutOfRangeException(parameterName, $"Permissions for service '{serviceName}' contain unknown flags.");
        }
    }

    private static void ValidateQueueName(string queueName)
        => ValidateName(queueName, nameof(queueName));

    private static void ValidateExchangeName(string exchangeName)
        => ValidateName(exchangeName, nameof(exchangeName));

    private static void ValidateRoutingKey(string routingKey)
        => ArgumentNullException.ThrowIfNull(routingKey);

    private static void ValidateBatchSubscribeOptions(BatchSubscribeOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.MaxMessages <= 0)
            throw new ArgumentOutOfRangeException(nameof(options.MaxMessages), "Maximum messages must be greater than zero.");

        if (options.MaxBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(options.MaxBytes), "Maximum bytes must be greater than zero.");

        if (options.MaxWaitTime <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(options.MaxWaitTime), "Maximum wait time must be greater than zero.");

        if (options.Weight <= 0)
            throw new ArgumentOutOfRangeException(nameof(options.Weight), "Subscriber weight must be greater than zero.");
    }
}