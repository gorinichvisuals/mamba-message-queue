namespace MambaMQ.Server.Handlers;

internal sealed class PublishMessageCommandHandler(IQueueManager queueManager) : ICommandHandler<PublishMessageCommand>
{
    public async Task Handle(PublishMessageCommand command, IClientConnection connection, CancellationToken cancellationToken)
       => await queueManager.PublishMessage(command.QueueName, command.MambaMessage, cancellationToken);
}