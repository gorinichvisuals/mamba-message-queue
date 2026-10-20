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
}
