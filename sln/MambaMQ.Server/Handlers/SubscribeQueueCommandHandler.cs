namespace MambaMQ.Server.Handlers;

internal sealed class SubscribeQueueCommandHandler(IQueueManager queueManager) : ICommandHandler<SubscribeQueueCommand>
{
    public async Task Handle(SubscribeQueueCommand command, IClientConnection connection, CancellationToken cancellationToken)
        => await queueManager.SubscribeQueue(command.QueueName, connection, cancellationToken);
}