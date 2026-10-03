namespace MambaMQ.Server.Handlers;

internal sealed class SubscribeQueueWithBatchCommandHandler(IQueueManager queueManager) : ICommandHandler<SubscribeQueueWithBatchCommand>
{
    public async Task Handle(SubscribeQueueWithBatchCommand command, IClientConnection connection, CancellationToken cancellationToken = default)
    {
        CommandResponse response = await queueManager.SubscribeQueueWithBatch(
            command.QueueName,
            connection,
            command.MaxMessages,
            command.MaxBytes,
            command.MaxWaitTime,
            command.Weight,
            cancellationToken);

        byte[] payload = CommandResponseEncoder.Encode(response);

        Frame frame = new(FrameType.CommandResponse, payload);

        await connection.SendAsync(frame, cancellationToken);
    }
}