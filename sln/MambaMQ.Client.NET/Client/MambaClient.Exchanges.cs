namespace MambaMQ.Client.NET;

internal sealed partial class MambaClient
{
    public async Task<ExchangeModel> CreateExchangeAsync(ExchangeCreateOptions exchangeCreateOptions, CancellationToken cancellationToken = default)
    {
        ValidateExchangeOptions(exchangeCreateOptions);
        await EnsureConnectedAsync(cancellationToken);

        ExchangeCreateCommand command = new(exchangeCreateOptions.Name, exchangeCreateOptions.IsDurable, exchangeCreateOptions.Type, exchangeCreateOptions.Permissions);

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

    public async Task<ExchangeModel> UpdateExchangeAsync(Guid exchangeId, ExchangeCreateOptions exchangeCreateOptions, CancellationToken cancellationToken = default)
    {
        ValidateId(exchangeId, nameof(exchangeId));
        ValidateExchangeOptions(exchangeCreateOptions);
        await EnsureConnectedAsync(cancellationToken);

        ExchangeUpdateCommand command = new(
            exchangeId,
            exchangeCreateOptions.Name,
            exchangeCreateOptions.IsDurable,
            exchangeCreateOptions.Type,
            exchangeCreateOptions.Permissions);

        return await SendCommandAsync(command, DecodeExchangeModel, cancellationToken);
    }

    public async Task DeleteExchangeAsync(Guid exchangeId, CancellationToken cancellationToken = default)
    {
        ValidateId(exchangeId, nameof(exchangeId));
        await EnsureConnectedAsync(cancellationToken);

        ExchangeDeleteCommand command = new(exchangeId);

        await SendCommandAsync(command, cancellationToken);
    }
    
    public async Task BindQueueToExchangeAsync(string exchangeName, string queueName, string routingKey, CancellationToken cancellationToken = default)
    {
        ValidateExchangeName(exchangeName);
        ValidateQueueName(queueName);
        ValidateRoutingKey(routingKey);
        await EnsureConnectedAsync(cancellationToken);

        ExchangeBindCommand command = new(exchangeName, queueName, routingKey);

        await SendCommandAsync(command, cancellationToken);
    }

    public async Task UnbindQueueFromExchangeAsync(string exchangeName, string queueName, string routingKey, CancellationToken cancellationToken = default)
    {
        ValidateExchangeName(exchangeName);
        ValidateQueueName(queueName);
        ValidateRoutingKey(routingKey);
        await EnsureConnectedAsync(cancellationToken);

        ExchangeUnbindCommand command = new(exchangeName, queueName, routingKey);

        await SendCommandAsync(command, cancellationToken);
    }

    public async Task PublishToExchangeAsync<T>(string exchangeName, string routingKey, T message, CancellationToken cancellationToken = default)
    {
        ValidateExchangeName(exchangeName);
        ValidateRoutingKey(routingKey);
        await EnsureConnectedAsync(cancellationToken);

        byte[] body = JsonSerializer.SerializeToUtf8Bytes(message);

        MambaMessage mambaMessage = new(body);

        ExchangeMessagePublishCommand command = new(exchangeName, routingKey, mambaMessage);

        await SendCommandAsync(command, cancellationToken);
    }
}