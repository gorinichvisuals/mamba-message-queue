namespace MambaMQ.Server.Handlers.Exchanges;

internal sealed class ExchangeUpdateCommandHandler(IExchangeManager exchangeManager) : ICommandHandler<ExchangeUpdateCommand>
{
    public async Task Handle(ExchangeUpdateCommand command, IClientConnection connection, CancellationToken cancellationToken = default)
    {
        ExchangeUpdateDto dto = new(
            command.ExchangeId,
            command.ExchangeName,
            command.IsDurable,
            command.ExchangeType,
            command.Permissions);

        CommandResponse response = await exchangeManager.UpdateExchange(dto, cancellationToken);

        byte[] payload = CommandResponseEncoder.Encode(response);

        Frame frame = new(FrameType.CommandResultResponse, payload);

        await connection.SendAsync(frame, cancellationToken);
    }
}