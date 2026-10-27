namespace MambaMQ.Server.Handlers.Queues;

internal sealed class QueueGetListCommandHandler(IQueueManager queueManager) : ICommandHandler<QueueGetListCommand>
{
    public async Task Handle(QueueGetListCommand listCommand, IClientConnection connection, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<QueueModel> queues = queueManager.GetQueues();

        GetQueuesResponse response = new()
        {
            Queues = queues
        };

        byte[] payload = GetQueuesResponseEncoder.Encode(response);

        Frame frame = new(FrameType.QueueGetListResponse, payload);

        await connection.SendAsync(frame, cancellationToken);
    }
}