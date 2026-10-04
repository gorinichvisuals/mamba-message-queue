namespace MambaMQ.Server.Handlers;

internal sealed class BindExchangeCommandHandler(IExchangeManager exchangeManager) : ICommandHandler<BindExchangeCommand>
{
    public async Task Handle(BindExchangeCommand command, IClientConnection connection, CancellationToken cancellationToken)
    {
        CommandResponse response = await exchangeManager.Bind(command.ExchangeName, command.QueueName, command.RoutingKey, connection);

        byte[] payload = CommandResponseEncoder.Encode(response);

        Frame frame = new(FrameType.CommandResponse, payload);

        await connection.SendAsync(frame, cancellationToken);
    }
}