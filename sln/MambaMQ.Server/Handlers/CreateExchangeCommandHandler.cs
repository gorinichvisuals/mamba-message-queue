namespace MambaMQ.Server.Handlers;

internal sealed class CreateExchangeCommandHandler(IExchangeManager exchangeManager) : ICommandHandler<CreateExchangeCommand>
{
    public async Task Handle(CreateExchangeCommand command, IClientConnection connection, CancellationToken cancellationToken)
    {
        ExchangeCreateDto newCreateDto = MaptoExchangeCreateDto(command);
        
        CommandResponse response = await exchangeManager.CreateExchange(newCreateDto);

        byte[] payload = CommandResponseEncoder.Encode(response);

        Frame frame = new(FrameType.CommandResponse, payload);

        await connection.SendAsync(frame, cancellationToken);
    }

    private ExchangeCreateDto MaptoExchangeCreateDto(CreateExchangeCommand command)
    {
        return new ExchangeCreateDto(
            command.ExchangeName,
            command.IsDurable,
            command.ExchangeType,
            command.Permissions);
    }
}