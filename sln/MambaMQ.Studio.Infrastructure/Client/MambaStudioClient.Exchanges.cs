namespace MambaMQ.Studio.Infrastructure.Client;

internal sealed partial class MambaStudioClient
{
    public async Task<GetExchangesResponse> GetExchangesAsync(CancellationToken cancellationToken = default)
    {
        ExchangeGetListCommand listCommand = new();

        byte[] payload = CommandEncoder.Encode(listCommand);

        Frame frame = new(FrameType.ExchangeGetListCommand, payload);

        Frame responseFrame = await SendRequestAsync(frame, cancellationToken);

        return responseFrame.Type is not FrameType.ExchangeGetListResponse
            ? throw new InvalidDataException($"Expected {FrameType.ExchangeGetListResponse} frame, but received {responseFrame.Type}.")
            : GetExchangesResponseDecoder.Decode(responseFrame.Payload.Span);
    }

    public async Task<CommandResponse<ExchangeModel>> CreateExchangeAsync(ExchangeOptions options, CancellationToken cancellationToken = default)
    {
        ExchangeCreateCommand command = new(
            options.Name,
            options.IsDurable,
            options.Type,
            options.Permissions);

        byte[] payload = CommandEncoder.Encode(command);

        Frame frame = new(FrameType.ExchangeCreateCommand, payload);

        Frame responseFrame = await SendRequestAsync(frame, cancellationToken);

        return responseFrame.Type is not FrameType.CommandResultResponse
            ? throw new InvalidDataException($"Expected {FrameType.CommandResultResponse} frame, but received {responseFrame.Type}.")
            : CommandResponseDecoder.Decode(responseFrame.Payload.Span, DecodeExchangeModel);
    }

    public async Task<CommandResponse> BindQueueToExchangeAsync(
        string exchangeName,
        string queueName,
        string routingKey,
        CancellationToken cancellationToken = default)
    {
        ExchangeBindCommand command = new(exchangeName, queueName, routingKey);

        byte[] payload = CommandEncoder.Encode(command);

        Frame frame = new(FrameType.ExchangeBindCommand, payload);

        Frame responseFrame = await SendRequestAsync(frame, cancellationToken);

        return responseFrame.Type is not FrameType.CommandResultResponse
            ? throw new InvalidDataException($"Expected {FrameType.CommandResultResponse} frame, but received {responseFrame.Type}.")
            : CommandResponseDecoder.Decode(responseFrame.Payload.Span);
    }

    public async Task<CommandResponse> UnbindQueueFromExchangeAsync(
        string exchangeName,
        string queueName,
        string routingKey,
        CancellationToken cancellationToken = default)
    {
        ExchangeUnbindCommand command = new(exchangeName, queueName, routingKey);

        byte[] payload = CommandEncoder.Encode(command);

        Frame frame = new(FrameType.ExchangeUnbindCommand, payload);

        Frame responseFrame = await SendRequestAsync(frame, cancellationToken);

        return responseFrame.Type is not FrameType.CommandResultResponse
            ? throw new InvalidDataException($"Expected {FrameType.CommandResultResponse} frame, but received {responseFrame.Type}.")
            : CommandResponseDecoder.Decode(responseFrame.Payload.Span);
    }

    public async Task<CommandResponse> DeleteExchangeAsync(Guid exchangeId, CancellationToken cancellationToken = default)
    {
        ExchangeDeleteCommand command = new(exchangeId);

        byte[] payload = CommandEncoder.Encode(command);

        Frame frame = new(FrameType.ExchangeDeleteCommand, payload);

        Frame responseFrame = await SendRequestAsync(frame, cancellationToken);

        return responseFrame.Type is not FrameType.CommandResultResponse
            ? throw new InvalidDataException($"Expected {FrameType.CommandResultResponse} frame, but received {responseFrame.Type}.")
            : CommandResponseDecoder.Decode(responseFrame.Payload.Span);
    }

    public async Task<CommandResponse<ExchangeModel>> UpdateExchangeAsync(Guid exchangeId, ExchangeOptions options, CancellationToken cancellationToken = default)
    {
        ExchangeUpdateCommand command = new(
            exchangeId,
            options.Name,
            options.IsDurable,
            options.Type,
            options.Permissions);

        byte[] payload = CommandEncoder.Encode(command);

        Frame frame = new(FrameType.ExchangeUpdateCommand, payload);

        Frame responseFrame = await SendRequestAsync(frame, cancellationToken);

        return responseFrame.Type is not FrameType.CommandResultResponse
            ? throw new InvalidDataException($"Expected {FrameType.CommandResultResponse} frame, but received {responseFrame.Type}.")
            : CommandResponseDecoder.Decode(responseFrame.Payload.Span, DecodeExchangeModel);
    }

    public async Task<CommandResponse> PublishMessageToExchangeAsync(
        string exchangeName,
        string routingKey,
        ReadOnlyMemory<byte> body,
        CancellationToken cancellationToken = default)
    {
        ExchangeMessagePublishCommand command = new(exchangeName, routingKey, new MambaMessage(body));

        byte[] payload = CommandEncoder.Encode(command);

        Frame frame = new(FrameType.ExchangeMessagePublishCommand, payload);

        Frame responseFrame = await SendRequestAsync(frame, cancellationToken);

        return responseFrame.Type is not FrameType.CommandResultResponse
            ? throw new InvalidDataException($"Expected {FrameType.CommandResultResponse} frame, but received {responseFrame.Type}.")
            : CommandResponseDecoder.Decode(responseFrame.Payload.Span);
    }

    private static ExchangeModel DecodeExchangeModel(ReadOnlySpan<byte> buffer)
    {
        int offset = 0;

        ExchangeModel exchange = ExchangeModelDecoder.Decode(buffer, ref offset);

        return offset == buffer.Length
            ? exchange
            : throw new InvalidDataException("Exchange response contains unexpected data.");
    }
}