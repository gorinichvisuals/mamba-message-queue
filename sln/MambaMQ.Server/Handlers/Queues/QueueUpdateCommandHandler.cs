namespace MambaMQ.Server.Handlers.Queues;

public sealed class QueueUpdateCommandHandler(IQueueManager queueManager) : ICommandHandler<QueueUpdateCommand>
{
    public async Task Handle(QueueUpdateCommand command, IClientConnection connection, CancellationToken cancellationToken = default)
    {
        QueueUpdateDto newQueueUpdateDto = MapToQueueUpdateDto(command);

        CommandResponse<QueueModel> response = await queueManager.UpdateQueue(newQueueUpdateDto, cancellationToken);

        byte[] payload = CommandResponseEncoder.Encode(response, QueueModelEncoder.Encode);

        Frame frame = new(FrameType.CommandResultResponse, payload);

        await connection.SendAsync(frame, cancellationToken);
    }

    private QueueUpdateDto MapToQueueUpdateDto(QueueUpdateCommand command)
    {
        return new QueueUpdateDto(
            command.QueueId,
            command.QueueName,
            command.IsDurable,
            command.LoadBalancingAlgorithm,
            command.Permissions,
            command.MessageRetentionEnabled,
            command.MessageRetentionPeriod);
    }
}