namespace MambaMQ.Server.Handlers;

internal sealed class SubscribeQueueCommandHandler(IQueueManager queueManager) : ICommandHandler<SubscribeQueueCommand>
{
    public async Task HandleAsync(SubscribeQueueCommand command, IClientConnection connection, CancellationToken cancellationToken)
        => await queueManager.SubscribeQueueAsync(command.QueueName, command.AutoAcknowledge, connection, cancellationToken);
}