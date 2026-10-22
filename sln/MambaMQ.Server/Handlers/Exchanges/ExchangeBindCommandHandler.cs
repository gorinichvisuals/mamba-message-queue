namespace MambaMQ.Server.Handlers.Exchanges;

internal sealed class ExchangeBindCommandHandler(IExchangeManager exchangeManager) : ICommandHandler<ExchangeBindCommand>
{
    public async Task Handle(ExchangeBindCommand command, IClientConnection connection, CancellationToken cancellationToken)
    {
        ExchangeBindingDto newBindingDto = MapToExchangeBindingDto(command);

        CommandResponse response = await exchangeManager.Bind(newBindingDto, connection);

        byte[] payload = CommandResponseEncoder.Encode(response);

        Frame frame = new(FrameType.CommandResultResponse, payload);

        await connection.SendAsync(frame, cancellationToken);
    }

    private ExchangeBindingDto MapToExchangeBindingDto(ExchangeBindCommand command)
    {
        return new ExchangeBindingDto(
            command.ExchangeName,
            command.QueueName,
            command.RoutingKey);
    }
}