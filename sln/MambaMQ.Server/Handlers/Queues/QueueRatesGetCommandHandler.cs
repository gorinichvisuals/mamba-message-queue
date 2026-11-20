namespace MambaMQ.Server.Handlers.Queues;

internal sealed class QueueRatesGetCommandHandler(IQueueManager queueManager) : ICommandHandler<QueueRatesGetCommand>
{
    public async Task Handle(QueueRatesGetCommand command, IClientConnection connection, CancellationToken cancellationToken = default)
    {
        CommandResponse<QueueRateStatisticsModel> response = queueManager.GetQueueRates(command.QueueId);
        
        byte[] payload = CommandResponseEncoder.Encode(response, QueueRateStatisticsModelEncoder.Encode);
        
        await connection.SendAsync(new Frame(FrameType.CommandResultResponse, payload), cancellationToken);
    }
}