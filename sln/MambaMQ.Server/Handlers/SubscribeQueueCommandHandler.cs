namespace MambaMQ.Server.Handlers;

internal sealed class SubscribeQueueCommandHandler(IQueueManager queueManager) : ICommandHandler<SubscribeQueueCommand>
{
    public async Task HandleAsync(SubscribeQueueCommand command, IClientConnection connection, CancellationToken cancellationToken)
    {
        await foreach (MessageDelivery delivery in queueManager.SubscribeQueueAsync(command.QueueName, command.AutoAcknowledge, connection.Id, cancellationToken))
        {
            byte[] payload = MessageEncoder.Encode(delivery.Message);

            Frame frame = new(FrameType.GetMessage, payload);

            await connection.SendAsync(frame, cancellationToken);
        }
    }
}