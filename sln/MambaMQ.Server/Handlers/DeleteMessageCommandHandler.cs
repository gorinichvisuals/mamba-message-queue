namespace MambaMQ.Server.Handlers;

internal sealed class DeleteMessageCommandHandler(IQueueManager queueManager) : ICommandHandler<DeleteMessageCommand>
{
    public async Task Handle(
        DeleteMessageCommand command,
        IClientConnection connection,
        CancellationToken cancellationToken)
    {
        CommandResponse response = await queueManager.DeleteMessage(
            command.QueueName,
            command.MessageId,
            connection,
            cancellationToken);

        byte[] payload = CommandResponseEncoder.Encode(response);

        Frame frame = new(FrameType.CommandResponse, payload);

        await connection.SendAsync(frame, cancellationToken);
    }
}