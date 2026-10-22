namespace MambaMQ.Server.Handlers.Queues;

internal sealed class QueueMessagePublishCommandHandler(IQueueManager queueManager) : ICommandHandler<QueueMessagePublishCommand>
{
    public async Task Handle(QueueMessagePublishCommand command, IClientConnection connection, CancellationToken cancellationToken)
    {
        CommandResponse response = await queueManager.PublishMessage(
            command.QueueName,
            command.MambaMessage,
            connection,
            cancellationToken);

        byte[] payload = CommandResponseEncoder.Encode(response);

        Frame frame = new(FrameType.CommandResultResponse, payload);

        await connection.SendAsync(frame, cancellationToken);
    }
}