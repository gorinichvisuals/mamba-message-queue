namespace MambaMQ.Server.Handlers;

internal sealed class UpdateExchangeCommandHandler(IExchangeManager exchangeManager) : ICommandHandler<UpdateExchangeCommand>
{
    public async Task Handle(UpdateExchangeCommand command, IClientConnection connection, CancellationToken cancellationToken = default)
    {
        ExchangeUpdateDto dto = new(
            command.ExchangeId,
            command.ExchangeName,
            command.IsDurable,
            command.ExchangeType,
            command.Permissions);

        CommandResponse response = await exchangeManager.UpdateExchange(dto, cancellationToken);
        
        byte[] payload = CommandResponseEncoder.Encode(response);
        
        Frame frame = new(FrameType.CommandResponse, payload);
       
        await connection.SendAsync(frame, cancellationToken);
    }
}