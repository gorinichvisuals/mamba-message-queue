namespace MambaMQ.Server.Handlers;

internal sealed class PublishMessageCommandHandler(IQueueManager queueManager) : ICommandHandler<PublishMessageCommand>
{
    public async Task HandleAsync(PublishMessageCommand command, IClientConnection connection, CancellationToken cancellationToken)
       => await queueManager.PublishMessage(
           command.QueueName, 
           command.IsDurable, 
           command.PersistMessages,
           command.MambaMessage, 
           cancellationToken);
}