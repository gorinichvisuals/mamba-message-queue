namespace MambaMQ.Server.Handlers;

internal sealed class GetExchangesCommandHandler(IExchangeManager exchangeManager) : ICommandHandler<GetExchangesCommand>
{
    public async Task Handle(GetExchangesCommand command, IClientConnection connection, CancellationToken cancellationToken = default)
    {
        GetExchangesResponse response = new()
        {
            Exchanges = exchangeManager.GetExchanges()
        };

        byte[] payload = GetExchangesResponseEncoder.Encode(response);
        Frame frame = new(FrameType.GetExchangesResponse, payload);

        await connection.SendAsync(frame, cancellationToken);
    }
}
