namespace MambaMQ.Server.Handlers;

internal sealed class SubscribeQueueCommandHandler(IQueueManager queueManager) : ICommandHandler<SubscribeQueueCommand>
{
    public async Task Handle(SubscribeQueueCommand command, IClientConnection connection, CancellationToken cancellationToken)
    {
        CommandResponse response = await queueManager.SubscribeQueue(command.QueueName, connection, cancellationToken);

        byte[] payload = CommandResponseEncoder.Encode(response);

        Frame frame = new(
            FrameType.CommandResponse,
            payload);

        await connection.SendAsync(frame, cancellationToken);
    }
}