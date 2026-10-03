namespace MambaMQ.Server.Handlers;

internal sealed class CreateExchangeCommandHandler(IExchangeManager exchangeManager) : ICommandHandler<CreateExchangeCommand>
{
    public async Task Handle(CreateExchangeCommand command, IClientConnection connection, CancellationToken cancellationToken)
        => await exchangeManager.CreateExchange(command.ExchangeName, command.IsDurable, command.ExchangeType);
}