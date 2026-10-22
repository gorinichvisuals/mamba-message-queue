namespace MambaMQ.Server.Handlers.Exchanges;

internal sealed class ExchangeGetCommandHandler(IExchangeManager exchangeManager) : ICommandHandler<ExchangeGetCommand>
{
    public async Task Handle(ExchangeGetCommand command, IClientConnection connection, CancellationToken cancellationToken = default)
    {
        GetExchangesResponse response = new()
        {
            Exchanges = exchangeManager.GetExchanges()
        };

        byte[] payload = GetExchangesResponseEncoder.Encode(response);
        Frame frame = new(FrameType.ExchangeGetResponse, payload);

        await connection.SendAsync(frame, cancellationToken);
    }
}