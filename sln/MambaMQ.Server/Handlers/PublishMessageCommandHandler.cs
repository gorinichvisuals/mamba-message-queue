namespace MambaMQ.Server.Handlers;

internal sealed class PublishMessageCommandHandler(IQueueManager queueManager) : ICommandHandler<PublishMessageCommand>
{
    public async Task Handle(PublishMessageCommand command, IClientConnection connection, CancellationToken cancellationToken)
    {
        CommandResponse response = await queueManager.PublishMessage(
            command.QueueName,
            command.MambaMessage,
            connection,
            cancellationToken);

        byte[] payload = CommandResponseEncoder.Encode(response);

        Frame frame = new(FrameType.CommandResponse, payload);

        await connection.SendAsync(frame, cancellationToken);
    }
}