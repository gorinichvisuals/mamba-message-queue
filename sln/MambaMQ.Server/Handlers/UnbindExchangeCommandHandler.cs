namespace MambaMQ.Server.Handlers;

internal sealed class UnbindExchangeCommandHandler(IExchangeManager exchangeManager) : ICommandHandler<UnbindExchangeCommand>
{
    public async Task Handle(UnbindExchangeCommand command, IClientConnection connection, CancellationToken cancellationToken)
    {
        ExchangeBindingDto newBindingDto = MapToExchangeBindingDto(command);

        CommandResponse response = await exchangeManager.Unbind(newBindingDto, connection);

        byte[] payload = CommandResponseEncoder.Encode(response);

        Frame frame = new(FrameType.CommandResponse, payload);

        await connection.SendAsync(frame, cancellationToken);
    }

    private ExchangeBindingDto MapToExchangeBindingDto(UnbindExchangeCommand command)
    {
        return new ExchangeBindingDto(
            command.ExchangeName,
            command.QueueName,
            command.RoutingKey);
    }
}