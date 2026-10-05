namespace MambaMQ.Server.Handlers;

internal sealed class GetQueuesCommandHandler(IQueueManager queueManager) : ICommandHandler<GetQueuesCommand>
{
    public async Task Handle(GetQueuesCommand command, IClientConnection connection, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<QueueModel> queues = queueManager.GetQueues();
        
        GetQueuesResponse response = new()
        {
            Queues = queues
        };
        
        byte[] payload = GetQueuesResponseEncoder.Encode(response);
        
        Frame frame = new(FrameType.GetQueuesResponse, payload);
        
        await connection.SendAsync(frame, cancellationToken);
    }
}