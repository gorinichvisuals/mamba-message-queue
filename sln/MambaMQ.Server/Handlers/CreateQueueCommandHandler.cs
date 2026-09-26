namespace MambaMQ.Server.Handlers;

internal sealed class CreateQueueCommandHandler(IQueueManager queueManager) : ICommandHandler<CreateQueueCommand>
{
    public async Task Handle(CreateQueueCommand command, IClientConnection connection, CancellationToken cancellationToken)
        => await queueManager.CreateQueue(
            command.QueueName, 
            command.IsDurable, 
            command.MessageRetentionEnabled, 
            command.MessageRetentionPeriod,
            command.LogRetentionEnabled, 
            (LogLevel)command.LogLevel,
            command.LogRetentionPeriod);
}