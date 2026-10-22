namespace MambaMQ.Studio.Infrastructure.Client;

internal sealed partial class MambaStudioClient
{
    public async Task<GetQueuesResponse> GetQueuesAsync(CancellationToken cancellationToken = default)
    {
        QueueGetCommand command = new();

        byte[] payload = CommandEncoder.Encode(command);

        Frame frame = new(FrameType.QueueGetCommand, payload);

        Frame responseFrame = await SendRequestAsync(frame, cancellationToken);

        return responseFrame.Type is not FrameType.QueueGetResponse
            ? throw new InvalidDataException($"Expected {FrameType.QueueGetResponse} frame, but received {responseFrame.Type}.")
            : GetQueuesResponseDecoder.Decode(responseFrame.Payload.Span);
    }

    public async Task<CommandResponse<QueueModel>> CreateQueueAsync(QueueOptions options, CancellationToken cancellationToken = default)
    {
        QueueCreateCommand command = new(
            options.QueueName,
            options.IsDurable,
            options.LoadBalancingAlgorithm,
            options.Permissions,
            options.MessageRetention.Enabled,
            options.MessageRetention.RetentionPeriod);

        byte[] payload = CommandEncoder.Encode(command);

        Frame frame = new(FrameType.QueueCreateCommand, payload);

        Frame responseFrame = await SendRequestAsync(frame, cancellationToken);

        return responseFrame.Type is not FrameType.CommandResultResponse
            ? throw new InvalidDataException($"Expected {FrameType.CommandResultResponse} frame, but received {responseFrame.Type}.")
            : CommandResponseDecoder.Decode(responseFrame.Payload.Span, QueueModelDecoder.Decode);
    }

    public async Task<CommandResponse> DeleteQueueAsync(Guid queueId, CancellationToken cancellationToken = default)
    {
        QueueDeleteCommand command = new(queueId);

        byte[] payload = CommandEncoder.Encode(command);

        Frame frame = new(FrameType.QueueDeleteCommand, payload);

        Frame responseFrame = await SendRequestAsync(frame, cancellationToken);

        return responseFrame.Type is not FrameType.CommandResultResponse
            ? throw new InvalidDataException($"Expected {FrameType.CommandResultResponse} frame, but received {responseFrame.Type}.")
            : CommandResponseDecoder.Decode(responseFrame.Payload.Span);
    }

    public async Task<CommandResponse<QueueModel>> UpdateQueueAsync(Guid queueId, QueueOptions options, CancellationToken cancellationToken = default)
    {
        QueueUpdateCommand command = new(
            queueId,
            options.QueueName,
            options.IsDurable,
            options.LoadBalancingAlgorithm,
            options.Permissions,
            options.MessageRetention.Enabled,
            options.MessageRetention.RetentionPeriod);

        byte[] payload = CommandEncoder.Encode(command);

        Frame frame = new(FrameType.QueueUpdateCommand, payload);

        Frame responseFrame = await SendRequestAsync(frame, cancellationToken);

        return responseFrame.Type is not FrameType.CommandResultResponse
            ? throw new InvalidDataException($"Expected {FrameType.CommandResultResponse} frame, but received {responseFrame.Type}.")
            : CommandResponseDecoder.Decode(responseFrame.Payload.Span, QueueModelDecoder.Decode);
    }
}