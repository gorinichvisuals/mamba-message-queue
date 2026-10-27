namespace MambaMQ.Server.Handlers.Exchanges;

internal sealed class ExchangeGetListCommandHandler(IExchangeManager exchangeManager) : ICommandHandler<ExchangeGetListCommand>
{
    public async Task Handle(ExchangeGetListCommand listCommand, IClientConnection connection, CancellationToken cancellationToken = default)
    {
        GetExchangesResponse response = new()
        {
            Exchanges = exchangeManager.GetExchanges()
        };

        byte[] payload = GetExchangesResponseEncoder.Encode(response);
        Frame frame = new(FrameType.ExchangeGetListResponse, payload);

        await connection.SendAsync(frame, cancellationToken);
    }
}