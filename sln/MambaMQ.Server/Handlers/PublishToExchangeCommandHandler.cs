namespace MambaMQ.Server.Handlers;

internal sealed class PublishToExchangeCommandHandler(
    IExchangeManager exchangeManager,
    IQueueManager queueManager) : ICommandHandler<PublishToExchangeCommand>
{
    public async Task Handle(PublishToExchangeCommand command, IClientConnection connection, CancellationToken cancellationToken)
    {
        IReadOnlyList<string> queueNames = exchangeManager.ResolveQueues(command.ExchangeName, command.RoutingKey);

        foreach (string queueName in queueNames)
            await queueManager.PublishMessage(queueName, command.Message, connection, cancellationToken);
    }
}