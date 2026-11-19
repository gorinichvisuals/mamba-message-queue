namespace MambaMQ.Server.Handlers.Queues;

internal sealed class QueueStatisticsGetCommandHandler(IQueueManager queueManager) : ICommandHandler<QueueStatisticsGetCommand>
{
    public async Task Handle(QueueStatisticsGetCommand command, IClientConnection connection, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<QueueStatisticsModel> statistics = queueManager.GetQueueStatistics(command.QueueId);
        byte[] payload = QueueStatisticsResponseEncoder.Encode(statistics);
        await connection.SendAsync(new Frame(FrameType.QueueStatisticsGetResponse, payload), cancellationToken);
    }
}