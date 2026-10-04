namespace MambaMQ.Server.Handlers;

internal sealed class CreateExchangeCommandHandler(IExchangeManager exchangeManager) : ICommandHandler<CreateExchangeCommand>
{
    public async Task Handle(CreateExchangeCommand command, IClientConnection connection, CancellationToken cancellationToken)
    {
        CommandResponse response = await exchangeManager.CreateExchange(
            command.ExchangeName, 
            command.IsDurable, 
            command.ExchangeType, 
            command.Permissions);

        byte[] payload = CommandResponseEncoder.Encode(response);

        Frame frame = new(FrameType.CommandResponse, payload);

        await connection.SendAsync(frame, cancellationToken);
    }
}