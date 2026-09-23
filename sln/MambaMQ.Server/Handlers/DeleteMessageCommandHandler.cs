namespace MambaMQ.Server.Handlers;

internal sealed class DeleteMessageCommandHandler(IQueueManager queueManager) : ICommandHandler<DeleteMessageCommand>
{
    public async Task HandleAsync(DeleteMessageCommand command, IClientConnection connection, CancellationToken cancellationToken)
        => await queueManager.DeleteMessage(command.QueueName, command.MessageId, cancellationToken);
}