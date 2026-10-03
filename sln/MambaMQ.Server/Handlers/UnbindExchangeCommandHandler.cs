namespace MambaMQ.Server.Handlers;

internal sealed class UnbindExchangeCommandHandler(IExchangeManager exchangeManager) : ICommandHandler<UnbindExchangeCommand>
{
    public async Task Handle(UnbindExchangeCommand command, IClientConnection connection, CancellationToken cancellationToken)
    {
        CommandResponse response = await exchangeManager.Unbind(command.ExchangeName, command.QueueName, command.RoutingKey);

        byte[] payload = CommandResponseEncoder.Encode(response);

        Frame frame = new(FrameType.CommandResponse, payload);

        await connection.SendAsync(frame, cancellationToken);
    }
}