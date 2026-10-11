namespace MambaMQ.Server.Handlers;

internal sealed class SubscribeQueueWithBatchCommandHandler(IQueueManager queueManager) : ICommandHandler<SubscribeQueueWithBatchCommand>
{
    public async Task Handle(SubscribeQueueWithBatchCommand command, IClientConnection connection, CancellationToken cancellationToken = default)
    {
        QueueSubscribeWithBatchDto newSubscribeWithBatchDto = MapToSubscribeWithBatchDto(command);
        
        CommandResponse response = await queueManager.SubscribeQueueWithBatch(newSubscribeWithBatchDto, connection, cancellationToken);

        byte[] payload = CommandResponseEncoder.Encode(response);

        Frame frame = new(FrameType.CommandResponse, payload);

        await connection.SendAsync(frame, cancellationToken);
    }

    private QueueSubscribeWithBatchDto MapToSubscribeWithBatchDto(SubscribeQueueWithBatchCommand command)
    {
        return new QueueSubscribeWithBatchDto(
            command.QueueName,
            command.MaxMessages,
            command.MaxBytes,
            command.MaxWaitTime,
            command.Weight);
    }
}