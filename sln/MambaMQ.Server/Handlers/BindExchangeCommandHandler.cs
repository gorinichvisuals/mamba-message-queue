namespace MambaMQ.Server.Handlers;

internal sealed class BindExchangeCommandHandler(IExchangeManager exchangeManager) : ICommandHandler<BindExchangeCommand>
{
    public async Task Handle(BindExchangeCommand command, IClientConnection connection, CancellationToken cancellationToken)
        => await exchangeManager.Bind(command.ExchangeName, command.QueueName, command.RoutingKey);
}