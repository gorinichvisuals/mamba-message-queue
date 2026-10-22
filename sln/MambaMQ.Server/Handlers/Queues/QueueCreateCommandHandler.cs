namespace MambaMQ.Server.Handlers.Queues;

internal sealed class QueueCreateCommandHandler(IQueueManager queueManager) : ICommandHandler<QueueCreateCommand>
{
    public async Task Handle(QueueCreateCommand command, IClientConnection connection, CancellationToken cancellationToken)
    {
        QueueCreateDto newQueueCreateDto = MapToQueueCreateDto(command);

        CommandResponse<QueueModel> response = await queueManager.CreateQueue(newQueueCreateDto);

        byte[] payload = CommandResponseEncoder.Encode(response, QueueModelEncoder.Encode);

        Frame frame = new(FrameType.CommandResultResponse, payload);

        await connection.SendAsync(frame, cancellationToken);
    }

    private QueueCreateDto MapToQueueCreateDto(QueueCreateCommand command)
    {
        return new QueueCreateDto(
            command.QueueName,
            command.IsDurable,
            command.LoadBalancingAlgorithm,
            command.Permissions,
            command.MessageRetentionEnabled,
            command.MessageRetentionPeriod);
    }
}