namespace MambaMQ.Server.Handlers;

internal class DeleteQueueCommandHandler(IQueueManager queueManager) : ICommandHandler<DeleteQueueCommand>
{
    public async Task Handle(DeleteQueueCommand command, IClientConnection connection, CancellationToken cancellationToken = default)
    {
        CommandResponse response = await queueManager.DeleteQueue(command.QueueId, connection, cancellationToken);

        byte[] payload = CommandResponseEncoder.Encode(response);

        Frame frame = new(FrameType.CommandResponse, payload);

        await connection.SendAsync(frame, cancellationToken);
    }
}