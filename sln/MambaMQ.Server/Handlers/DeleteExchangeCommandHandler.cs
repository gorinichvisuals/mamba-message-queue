namespace MambaMQ.Server.Handlers;

internal sealed class DeleteExchangeCommandHandler(IExchangeManager exchangeManager) : ICommandHandler<DeleteExchangeCommand>
{
    public async Task Handle(DeleteExchangeCommand command, IClientConnection connection, CancellationToken cancellationToken = default)
    {
        CommandResponse response = await exchangeManager.DeleteExchange(command.ExchangeId, connection, cancellationToken);
        
        byte[] payload = CommandResponseEncoder.Encode(response);
        
        Frame frame = new(FrameType.CommandResponse, payload);
        
        await connection.SendAsync(frame, cancellationToken);
    }
}
