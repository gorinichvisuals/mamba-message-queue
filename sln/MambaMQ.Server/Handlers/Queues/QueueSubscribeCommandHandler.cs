namespace MambaMQ.Server.Handlers.Queues;

internal sealed class QueueSubscribeCommandHandler(IQueueManager queueManager) : ICommandHandler<QueueSubscribeCommand>
{
    public async Task Handle(QueueSubscribeCommand command, IClientConnection connection, CancellationToken cancellationToken)
    {
        CommandResponse response = await queueManager.SubscribeQueue(command.QueueName, connection, cancellationToken);

        byte[] payload = CommandResponseEncoder.Encode(response);

        Frame frame = new(
            FrameType.CommandResultResponse,
            payload);

        await connection.SendAsync(frame, cancellationToken);
    }
}