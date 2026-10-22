namespace MambaMQ.Server.Handlers.Queues;

internal class QueueDeleteCommandHandler(IQueueManager queueManager) : ICommandHandler<QueueDeleteCommand>
{
    public async Task Handle(QueueDeleteCommand command, IClientConnection connection, CancellationToken cancellationToken = default)
    {
        CommandResponse response = await queueManager.DeleteQueue(command.QueueId, connection, cancellationToken);

        byte[] payload = CommandResponseEncoder.Encode(response);

        Frame frame = new(FrameType.CommandResultResponse, payload);

        await connection.SendAsync(frame, cancellationToken);
    }
}