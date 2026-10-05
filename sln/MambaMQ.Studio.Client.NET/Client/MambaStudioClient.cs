namespace MambaMQ.Studio.Client.NET.Client;

internal sealed class MambaStudioClient(
    IConnection connection,
    IAuthenticateService authenticateService) : IMambaStudioClient
{
    public async Task ConnectAsync(string host, int port, string username, string password, CancellationToken cancellationToken = default)
    {
        await connection.ConnectAsync(host, port, cancellationToken);

        await authenticateService.AuthenticateAsync(username, password, cancellationToken);
    }

    public async ValueTask DisconnectAsync(CancellationToken cancellationToken = default)
        => await connection.DisposeAsync();

    public async Task<GetQueuesResponse> GetQueuesAsync(CancellationToken cancellationToken = default)
    {
        GetQueuesCommand command = new();

        byte[] payload = CommandEncoder.Encode(command);
        
        Frame frame = new(FrameType.GetQueuesCommand, payload);

        await connection.SendAsync(FrameEncoder.Encode(frame), cancellationToken);
        
        Frame responseFrame = await FrameReader.ReadAsync(connection, 1048576, cancellationToken);

        return responseFrame.Type is not FrameType.GetQueuesResponse 
            ? throw new InvalidDataException($"Expected {FrameType.GetQueuesResponse} frame, but received {responseFrame.Type}.") 
            : GetQueuesResponseDecoder.Decode(responseFrame.Payload.Span);
    }
    
    public async Task<CommandResponse<QueueModel>> CreateQueueAsync(QueueOptions options,
        CancellationToken cancellationToken = default)
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

        await connection.SendAsync(FrameEncoder.Encode(frame), cancellationToken);

        Frame responseFrame = await FrameReader.ReadAsync(connection, 1048576, cancellationToken);

        return responseFrame.Type is not FrameType.CommandResponse 
            ? throw new InvalidDataException($"Expected {FrameType.CommandResponse} frame, but received {responseFrame.Type}.") 
            : CommandResponseDecoder.Decode(responseFrame.Payload.Span, QueueModelDecoder.Decode);
    }

    public async Task<CommandResponse> DeleteQueueAsync(Guid queueId, CancellationToken cancellationToken = default)
    {
        DeleteQueueCommand command = new(queueId);

        byte[] payload = CommandEncoder.Encode(command);

        Frame frame = new(FrameType.DeleteQueueCommand, payload);

        await connection.SendAsync(FrameEncoder.Encode(frame), cancellationToken);

        Frame responseFrame = await FrameReader.ReadAsync(connection, 1048576, cancellationToken);

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

        await connection.SendAsync(FrameEncoder.Encode(frame), cancellationToken);

        Frame responseFrame = await FrameReader.ReadAsync(connection, 1048576, cancellationToken);

        return responseFrame.Type is not FrameType.CommandResponse
            ? throw new InvalidDataException($"Expected {FrameType.CommandResponse} frame, but received {responseFrame.Type}.")
            : CommandResponseDecoder.Decode(responseFrame.Payload.Span, QueueModelDecoder.Decode);
    }
}