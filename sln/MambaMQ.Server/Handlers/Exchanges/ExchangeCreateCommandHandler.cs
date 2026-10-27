namespace MambaMQ.Server.Handlers.Exchanges;

internal sealed class ExchangeCreateCommandHandler(IExchangeManager exchangeManager) : ICommandHandler<ExchangeCreateCommand>
{
    public async Task Handle(ExchangeCreateCommand command, IClientConnection connection, CancellationToken cancellationToken)
    {
        ExchangeCreateDto newCreateDto = MaptoExchangeCreateDto(command);

        CommandResponse<ExchangeModel> response = await exchangeManager.CreateExchange(newCreateDto);

        byte[] payload = CommandResponseEncoder.Encode(response, ExchangeModelEncoder.Encode);

        Frame frame = new(FrameType.CommandResultResponse, payload);

        await connection.SendAsync(frame, cancellationToken);
    }

    private ExchangeCreateDto MaptoExchangeCreateDto(ExchangeCreateCommand command)
    {
        return new ExchangeCreateDto(
            command.ExchangeName,
            command.IsDurable,
            command.ExchangeType,
            command.Permissions);
    }
}