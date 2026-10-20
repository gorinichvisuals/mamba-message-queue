namespace MambaMQ.Studio.Infrastructure.Client;

internal sealed partial class MambaStudioClient
{
    public async Task<GetExchangesResponse> GetExchangesAsync(CancellationToken cancellationToken = default)
    {
        GetExchangesCommand command = new();

        byte[] payload = CommandEncoder.Encode(command);

        Frame frame = new(FrameType.GetExchangesCommand, payload);

        await connection.SendAsync(FrameEncoder.Encode(frame), cancellationToken);

        Frame responseFrame = await FrameReader.ReadAsync(connection, 1048576, cancellationToken);

        return responseFrame.Type is not FrameType.GetExchangesResponse
            ? throw new InvalidDataException($"Expected {FrameType.GetExchangesResponse} frame, but received {responseFrame.Type}.")
            : GetExchangesResponseDecoder.Decode(responseFrame.Payload.Span);
    }

    public async Task<CommandResponse> CreateExchangeAsync(ExchangeOptions options, CancellationToken cancellationToken = default)
    {
        CreateExchangeCommand command = new(
            options.Name,
            options.IsDurable,
            options.Type,
            options.Permissions);

        byte[] payload = CommandEncoder.Encode(command);
        Frame frame = new(FrameType.CreateExchange, payload);

        await connection.SendAsync(FrameEncoder.Encode(frame), cancellationToken);

        Frame responseFrame = await FrameReader.ReadAsync(connection, 1048576, cancellationToken);

        return responseFrame.Type is not FrameType.CommandResponse
            ? throw new InvalidDataException($"Expected {FrameType.CommandResponse} frame, but received {responseFrame.Type}.")
            : CommandResponseDecoder.Decode(responseFrame.Payload.Span);
    }

    public async Task<CommandResponse> BindQueueToExchangeAsync(
        string exchangeName,
        string queueName,
        string routingKey,
        CancellationToken cancellationToken = default)
    {
        BindExchangeCommand command = new(exchangeName, queueName, routingKey);

        byte[] payload = CommandEncoder.Encode(command);

        Frame frame = new(FrameType.BindExchange, payload);

        await connection.SendAsync(FrameEncoder.Encode(frame), cancellationToken);

        Frame responseFrame = await FrameReader.ReadAsync(connection, 1048576, cancellationToken);

        return responseFrame.Type is not FrameType.CommandResponse
            ? throw new InvalidDataException($"Expected {FrameType.CommandResponse} frame, but received {responseFrame.Type}.")
            : CommandResponseDecoder.Decode(responseFrame.Payload.Span);
    }

    public async Task<CommandResponse> UnbindQueueFromExchangeAsync(
        string exchangeName,
        string queueName,
        string routingKey,
        CancellationToken cancellationToken = default)
    {
        UnbindExchangeCommand command = new(exchangeName, queueName, routingKey);

        byte[] payload = CommandEncoder.Encode(command);

        Frame frame = new(FrameType.UnbindExchange, payload);

        await connection.SendAsync(FrameEncoder.Encode(frame), cancellationToken);

        Frame responseFrame = await FrameReader.ReadAsync(connection, 1048576, cancellationToken);

        return responseFrame.Type is not FrameType.CommandResponse
            ? throw new InvalidDataException($"Expected {FrameType.CommandResponse} frame, but received {responseFrame.Type}.")
            : CommandResponseDecoder.Decode(responseFrame.Payload.Span);
    }

    public async Task<CommandResponse> DeleteExchangeAsync(Guid exchangeId, CancellationToken cancellationToken = default)
    {
        DeleteExchangeCommand command = new(exchangeId);

        byte[] payload = CommandEncoder.Encode(command);

        Frame frame = new(FrameType.DeleteExchangeCommand, payload);

        await connection.SendAsync(FrameEncoder.Encode(frame), cancellationToken);

        Frame responseFrame = await FrameReader.ReadAsync(connection, 1048576, cancellationToken);

        return responseFrame.Type is not FrameType.CommandResponse
            ? throw new InvalidDataException($"Expected {FrameType.CommandResponse} frame, but received {responseFrame.Type}.")
            : CommandResponseDecoder.Decode(responseFrame.Payload.Span);
    }

    public async Task<CommandResponse> UpdateExchangeAsync(Guid exchangeId, ExchangeOptions options, CancellationToken cancellationToken = default)
    {
        UpdateExchangeCommand command = new(
            exchangeId,
            options.Name,
            options.IsDurable,
            options.Type,
            options.Permissions);

        byte[] payload = CommandEncoder.Encode(command);

        Frame frame = new(FrameType.UpdateExchangeCommand, payload);

        await connection.SendAsync(FrameEncoder.Encode(frame), cancellationToken);

        Frame responseFrame = await FrameReader.ReadAsync(connection, 1048576, cancellationToken);

        return responseFrame.Type is not FrameType.CommandResponse
            ? throw new InvalidDataException($"Expected {FrameType.CommandResponse} frame, but received {responseFrame.Type}.")
            : CommandResponseDecoder.Decode(responseFrame.Payload.Span);
    }

    public async Task<CommandResponse> PublishMessageToExchangeAsync(
        string exchangeName,
        string routingKey,
        ReadOnlyMemory<byte> body,
        CancellationToken cancellationToken = default)
    {
        PublishToExchangeCommand command = new(exchangeName, routingKey, new MambaMessage(body));
        byte[] payload = CommandEncoder.Encode(command);
        Frame frame = new(FrameType.PublishToExchange, payload);

        await connection.SendAsync(FrameEncoder.Encode(frame), cancellationToken);

        Frame responseFrame = await FrameReader.ReadAsync(connection, 1048576, cancellationToken);

        return responseFrame.Type is not FrameType.CommandResponse
            ? throw new InvalidDataException($"Expected {FrameType.CommandResponse} frame, but received {responseFrame.Type}.")
            : CommandResponseDecoder.Decode(responseFrame.Payload.Span);
    }
}
