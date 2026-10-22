namespace MambaMQ.Server.Handlers.Queues;

internal sealed class QueueMessagesDeleteCommandHandler(IQueueManager queueManager) : ICommandHandler<QueueMessagesDeleteCommand>
{
    public async Task Handle(QueueMessagesDeleteCommand command, IClientConnection connection, CancellationToken cancellationToken)
    {
        CommandResponse response = await queueManager.DeleteMessages(
            command.QueueName,
            command.MessageIds,
            connection,
            cancellationToken);

        byte[] payload = CommandResponseEncoder.Encode(response);

        Frame frame = new(FrameType.CommandResultResponse, payload);

        await connection.SendAsync(frame, cancellationToken);
    }
}