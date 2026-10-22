namespace MambaMQ.Server.Handlers.Queues;

internal sealed class QueueMessageDeleteCommandHandler(IQueueManager queueManager) : ICommandHandler<QueueMessageDeleteCommand>
{
    public async Task Handle(QueueMessageDeleteCommand command, IClientConnection connection, CancellationToken cancellationToken)
    {
        CommandResponse response = await queueManager.DeleteMessage(
            command.QueueName,
            command.MessageId,
            connection,
            cancellationToken);

        byte[] payload = CommandResponseEncoder.Encode(response);

        Frame frame = new(FrameType.CommandResultResponse, payload);

        await connection.SendAsync(frame, cancellationToken);
    }
}