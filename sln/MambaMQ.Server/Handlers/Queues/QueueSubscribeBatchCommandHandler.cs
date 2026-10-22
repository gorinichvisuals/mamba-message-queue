namespace MambaMQ.Server.Handlers.Queues;

internal sealed class QueueSubscribeBatchCommandHandler(IQueueManager queueManager) : ICommandHandler<QueueSubscribeBatchCommand>
{
    public async Task Handle(QueueSubscribeBatchCommand command, IClientConnection connection, CancellationToken cancellationToken = default)
    {
        QueueSubscribeWithBatchDto newSubscribeWithBatchDto = MapToSubscribeWithBatchDto(command);

        CommandResponse response = await queueManager.SubscribeQueueWithBatch(newSubscribeWithBatchDto, connection, cancellationToken);

        byte[] payload = CommandResponseEncoder.Encode(response);

        Frame frame = new(FrameType.CommandResultResponse, payload);

        await connection.SendAsync(frame, cancellationToken);
    }

    private QueueSubscribeWithBatchDto MapToSubscribeWithBatchDto(QueueSubscribeBatchCommand command)
    {
        return new QueueSubscribeWithBatchDto(
            command.QueueName,
            command.MaxMessages,
            command.MaxBytes,
            command.MaxWaitTime,
            command.Weight);
    }
}