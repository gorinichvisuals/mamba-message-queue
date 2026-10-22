namespace MambaMQ.Server.Handlers.Exchanges;

internal sealed class ExchangeUnbindCommandHandler(IExchangeManager exchangeManager) : ICommandHandler<ExchangeUnbindCommand>
{
    public async Task Handle(ExchangeUnbindCommand command, IClientConnection connection, CancellationToken cancellationToken)
    {
        ExchangeBindingDto newBindingDto = MapToExchangeBindingDto(command);

        CommandResponse response = await exchangeManager.Unbind(newBindingDto, connection);

        byte[] payload = CommandResponseEncoder.Encode(response);

        Frame frame = new(FrameType.CommandResultResponse, payload);

        await connection.SendAsync(frame, cancellationToken);
    }

    private ExchangeBindingDto MapToExchangeBindingDto(ExchangeUnbindCommand command)
    {
        return new ExchangeBindingDto(
            command.ExchangeName,
            command.QueueName,
            command.RoutingKey);
    }
}