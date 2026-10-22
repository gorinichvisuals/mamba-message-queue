namespace MambaMQ.Studio.Infrastructure.Client;

internal sealed partial class MambaStudioClient
{
    public async Task<CommandResponse> PublishMessageToQueueAsync(string queueName, ReadOnlyMemory<byte> body, CancellationToken cancellationToken = default)
    {
        MambaMessage message = new(body);

        QueueMessagePublishCommand command = new(queueName, message);

        byte[] payload = CommandEncoder.Encode(command);

        Frame frame = new(FrameType.QueueMessagePublishCommand, payload);

        Frame responseFrame = await SendRequestAsync(frame, cancellationToken);

        return responseFrame.Type is not FrameType.CommandResultResponse
            ? throw new InvalidDataException($"Expected {FrameType.CommandResultResponse} frame, but received {responseFrame.Type}.")
            : CommandResponseDecoder.Decode(responseFrame.Payload.Span);
    }

    public async Task<CommandResponse<GetMessagesResponse>> GetMessagesAsync(string queueName, int skip, int take, CancellationToken cancellationToken = default)
    {
        QueueMessagesGetCommand command = new(queueName, skip, take);

        byte[] payload = CommandEncoder.Encode(command);

        Frame frame = new(FrameType.QueueMessagesGetCommand, payload);

        Frame responseFrame = await SendRequestAsync(frame, cancellationToken);

        return responseFrame.Type switch
        {
            FrameType.CommandResultResponse => CommandResponseDecoder.Decode(responseFrame.Payload.Span, GetMessagesResponseDecoder.Decode),

            FrameType.QueueMessagesGetResponse => CommandResponse<GetMessagesResponse>.Success(GetMessagesResponseDecoder.Decode(responseFrame.Payload.Span)),

            _ => throw new InvalidDataException($"Expected {FrameType.CommandResultResponse} or {FrameType.QueueMessagesGetResponse} frame, but received {responseFrame.Type}.")
        };
    }

    public async Task<CommandResponse> DeleteMessagesAsync(string queueName, ICollection<Guid> messageIds, CancellationToken cancellationToken = default)
    {
        QueueMessagesDeleteCommand command = new(queueName, messageIds);

        byte[] payload = CommandEncoder.Encode(command);

        Frame frame = new(FrameType.QueueMessagesDeleteCommand, payload);

        Frame responseFrame = await SendRequestAsync(frame, cancellationToken);

        return responseFrame.Type is not FrameType.CommandResultResponse
            ? throw new InvalidDataException($"Expected {FrameType.CommandResultResponse} frame, but received {responseFrame.Type}.")
            : CommandResponseDecoder.Decode(responseFrame.Payload.Span);
    }
}