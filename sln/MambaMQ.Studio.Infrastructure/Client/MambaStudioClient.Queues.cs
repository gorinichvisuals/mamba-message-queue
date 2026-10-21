namespace MambaMQ.Studio.Infrastructure.Client;

internal sealed partial class MambaStudioClient
{
    public async Task<GetQueuesResponse> GetQueuesAsync(CancellationToken cancellationToken = default)
    {
        GetQueuesCommand command = new();

        byte[] payload = CommandEncoder.Encode(command);
        
        Frame frame = new(FrameType.GetQueuesCommand, payload);

        Frame responseFrame = await SendRequestAsync(frame, cancellationToken);

        return responseFrame.Type is not FrameType.GetQueuesResponse 
            ? throw new InvalidDataException($"Expected {FrameType.GetQueuesResponse} frame, but received {responseFrame.Type}.") 
            : GetQueuesResponseDecoder.Decode(responseFrame.Payload.Span);
    }
    
    public async Task<CommandResponse<QueueModel>> CreateQueueAsync(QueueOptions options, CancellationToken cancellationToken = default)
    {
        CreateQueueCommand command = new(
            options.QueueName,
            options.IsDurable,
            options.LoadBalancingAlgorithm,
            options.Permissions,
            options.MessageRetention.Enabled,
            options.MessageRetention.RetentionPeriod);

        byte[] payload = CommandEncoder.Encode(command);

        Frame frame = new(FrameType.CreateQueue, payload);

        Frame responseFrame = await SendRequestAsync(frame, cancellationToken);

        return responseFrame.Type is not FrameType.CommandResponse 
            ? throw new InvalidDataException($"Expected {FrameType.CommandResponse} frame, but received {responseFrame.Type}.") 
            : CommandResponseDecoder.Decode(responseFrame.Payload.Span, QueueModelDecoder.Decode);
    }

    public async Task<CommandResponse> DeleteQueueAsync(Guid queueId, CancellationToken cancellationToken = default)
    {
        DeleteQueueCommand command = new(queueId);

        byte[] payload = CommandEncoder.Encode(command);

        Frame frame = new(FrameType.DeleteQueueCommand, payload);

        Frame responseFrame = await SendRequestAsync(frame, cancellationToken);

        return responseFrame.Type is not FrameType.CommandResponse
            ? throw new InvalidDataException($"Expected {FrameType.CommandResponse} frame, but received {responseFrame.Type}.")
            : CommandResponseDecoder.Decode(responseFrame.Payload.Span);
    }
    
    public async Task<CommandResponse<QueueModel>> UpdateQueueAsync(Guid queueId, QueueOptions options, CancellationToken cancellationToken = default)
    {
        UpdateQueueCommand command = new(
            queueId,
            options.QueueName,
            options.IsDurable,
            options.LoadBalancingAlgorithm,
            options.Permissions,
            options.MessageRetention.Enabled,
            options.MessageRetention.RetentionPeriod);

        byte[] payload = CommandEncoder.Encode(command);

        Frame frame = new(FrameType.UpdateQueueCommand, payload);

        Frame responseFrame = await SendRequestAsync(frame, cancellationToken);

        return responseFrame.Type is not FrameType.CommandResponse
            ? throw new InvalidDataException($"Expected {FrameType.CommandResponse} frame, but received {responseFrame.Type}.")
            : CommandResponseDecoder.Decode(responseFrame.Payload.Span, QueueModelDecoder.Decode);
    }
}
