namespace MambaMQ.Server.Handlers;

internal sealed class CreateQueueCommandHandler(IQueueManager queueManager) : ICommandHandler<CreateQueueCommand>
{
    public async Task Handle(CreateQueueCommand command, IClientConnection connection, CancellationToken cancellationToken)
    {
        CommandResponse<QueueModel> response = await queueManager.CreateQueue(
            command.QueueName,
            command.IsDurable,
            command.LoadBalancingAlgorithm,
            command.Permissions,
            command.MessageRetentionEnabled,
            command.MessageRetentionPeriod);

        byte[] payload = CommandResponseEncoder.Encode(response, QueueModelEncoder.Encode);

        Frame frame = new(FrameType.CommandResponse, payload);

        await connection.SendAsync(frame, cancellationToken);
    }
}