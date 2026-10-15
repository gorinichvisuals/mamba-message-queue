namespace MambaMQ.Server.Handlers;

internal sealed class DeleteMessagesCommandHandler(IQueueManager queueManager) : ICommandHandler<DeleteMessagesCommand>
{
    public async Task Handle(DeleteMessagesCommand command, IClientConnection connection, CancellationToken cancellationToken)
    {
        CommandResponse response = await queueManager.DeleteMessages(
            command.QueueName,
            command.MessageIds,
            connection,
            cancellationToken);

        byte[] payload = CommandResponseEncoder.Encode(response);

        Frame frame = new(FrameType.CommandResponse, payload);

        await connection.SendAsync(frame, cancellationToken);
    }
}