namespace MambaMQ.Server.Handlers.Exchanges;

internal sealed class ExchangeDeleteCommandHandler(IExchangeManager exchangeManager) : ICommandHandler<ExchangeDeleteCommand>
{
    public async Task Handle(ExchangeDeleteCommand command, IClientConnection connection, CancellationToken cancellationToken = default)
    {
        CommandResponse response = await exchangeManager.DeleteExchange(command.ExchangeId, connection, cancellationToken);

        byte[] payload = CommandResponseEncoder.Encode(response);

        Frame frame = new(FrameType.CommandResultResponse, payload);

        await connection.SendAsync(frame, cancellationToken);
    }
}