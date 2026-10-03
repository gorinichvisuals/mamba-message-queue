namespace MambaMQ.Server.Handlers;

internal sealed class UnbindExchangeCommandHandler(IExchangeManager exchangeManager) : ICommandHandler<UnbindExchangeCommand>
{
    public async Task Handle(UnbindExchangeCommand command, IClientConnection connection, CancellationToken cancellationToken)
        => await exchangeManager.Unbind(command.ExchangeName, command.QueueName, command.RoutingKey);
}