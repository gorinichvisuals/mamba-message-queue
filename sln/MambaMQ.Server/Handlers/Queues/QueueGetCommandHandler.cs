namespace MambaMQ.Server.Handlers.Queues;

internal sealed class QueueGetCommandHandler(IQueueManager queueManager) : ICommandHandler<QueueGetCommand>
{
    public async Task Handle(QueueGetCommand command, IClientConnection connection, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<QueueModel> queues = queueManager.GetQueues();

        GetQueuesResponse response = new()
        {
            Queues = queues
        };

        byte[] payload = GetQueuesResponseEncoder.Encode(response);

        Frame frame = new(FrameType.QueueGetResponse, payload);

        await connection.SendAsync(frame, cancellationToken);
    }
}