namespace MambaMQ.Server.Handlers;

internal sealed class BindExchangeCommandHandler(IExchangeManager exchangeManager) : ICommandHandler<BindExchangeCommand>
{
    public async Task Handle(BindExchangeCommand command, IClientConnection connection, CancellationToken cancellationToken)
    {
        ExchangeBindingDto newBindingDto = MapToExchangeBindingDto(command);
        
        CommandResponse response = await exchangeManager.Bind(newBindingDto, connection);

        byte[] payload = CommandResponseEncoder.Encode(response);

        Frame frame = new(FrameType.CommandResponse, payload);

        await connection.SendAsync(frame, cancellationToken);
    }

    private ExchangeBindingDto MapToExchangeBindingDto(BindExchangeCommand command)
    {
        return new ExchangeBindingDto(
            command.ExchangeName, 
            command.QueueName, 
            command.RoutingKey);
    }
}