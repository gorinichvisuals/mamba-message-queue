namespace MambaMQ.Server.Handlers;

public sealed class UpdateQueueCommandHandler(IQueueManager queueManager) : ICommandHandler<UpdateQueueCommand>
{
    public async Task Handle(UpdateQueueCommand command, IClientConnection connection, CancellationToken cancellationToken = default)
    {
        QueueUpdateDto newQueueUpdateDto = MapToQueueUpdateDto(command);
        
        CommandResponse<QueueModel> response = await queueManager.UpdateQueue(newQueueUpdateDto, cancellationToken);

        byte[] payload = CommandResponseEncoder.Encode(response, QueueModelEncoder.Encode);
        
        Frame frame = new(FrameType.CommandResponse, payload);

        await connection.SendAsync(frame, cancellationToken);
    }

    private QueueUpdateDto MapToQueueUpdateDto(UpdateQueueCommand command)
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