namespace MambaMQ.Studio.Infrastructure.Client;

internal sealed partial class MambaStudioClient
{
    public async Task<CommandResponse> PublishMessageToQueueAsync(string queueName, ReadOnlyMemory<byte> body, CancellationToken cancellationToken = default)
    {
        MambaMessage message = new(body);

        PublishMessageCommand command = new(queueName, message);

        byte[] payload = CommandEncoder.Encode(command);

        Frame frame = new(FrameType.PublishMessage, payload);

        await connection.SendAsync(FrameEncoder.Encode(frame), cancellationToken);

        Frame responseFrame = await FrameReader.ReadAsync(connection, 1048576, cancellationToken);

        return responseFrame.Type is not FrameType.CommandResponse
            ? throw new InvalidDataException($"Expected {FrameType.CommandResponse} frame, but received {responseFrame.Type}.")
            : CommandResponseDecoder.Decode(responseFrame.Payload.Span);
    }

    public async Task<CommandResponse<GetMessagesResponse>> GetMessagesAsync(string queueName, int skip, int take, CancellationToken cancellationToken = default)
    {
        GetMessagesCommand command = new(queueName, skip, take);

        byte[] payload = CommandEncoder.Encode(command);

        Frame frame = new(FrameType.GetMessagesCommand, payload);

        await connection.SendAsync(FrameEncoder.Encode(frame), cancellationToken);

        Frame responseFrame = await FrameReader.ReadAsync(connection, 1048576, cancellationToken);

        return responseFrame.Type switch
        {
            FrameType.CommandResponse => CommandResponseDecoder.Decode(responseFrame.Payload.Span, GetMessagesResponseDecoder.Decode),

            FrameType.GetMessagesResponse => CommandResponse<GetMessagesResponse>.Success(GetMessagesResponseDecoder.Decode(responseFrame.Payload.Span)),

            _ => throw new InvalidDataException($"Expected {FrameType.CommandResponse} or {FrameType.GetMessagesResponse} frame, but received {responseFrame.Type}.")
        };
    }

    public async Task<CommandResponse> DeleteMessagesAsync(string queueName, ICollection<Guid> messageIds, CancellationToken cancellationToken = default)
    {
        DeleteMessagesCommand command = new(queueName, messageIds);

        byte[] payload = CommandEncoder.Encode(command);

        Frame frame = new(FrameType.DeleteSelectedMessagesCommand, payload);

        await connection.SendAsync(FrameEncoder.Encode(frame), cancellationToken);

        Frame responseFrame = await FrameReader.ReadAsync(connection, 1048576, cancellationToken);

        return responseFrame.Type is not FrameType.CommandResponse
            ? throw new InvalidDataException($"Expected {FrameType.CommandResponse} frame, but received {responseFrame.Type}.")
            : CommandResponseDecoder.Decode(responseFrame.Payload.Span);
    }
}