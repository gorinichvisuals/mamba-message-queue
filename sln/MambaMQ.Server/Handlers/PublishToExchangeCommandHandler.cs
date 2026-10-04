namespace MambaMQ.Server.Handlers;

internal sealed class PublishToExchangeCommandHandler(
    IExchangeManager exchangeManager,
    IQueueManager queueManager) : ICommandHandler<PublishToExchangeCommand>
{
    public async Task Handle(PublishToExchangeCommand command, IClientConnection connection, CancellationToken cancellationToken)
    {
        CommandResponse<IReadOnlyList<string>> response = exchangeManager.ResolveQueues(command.ExchangeName, command.RoutingKey, connection);

        if (!response.IsSucceed)
        {
            byte[] payload = CommandResponseEncoder.Encode(response);

            Frame frame = new(FrameType.CommandResponse, payload);

            await connection.SendAsync(frame, cancellationToken);

            return;
        }

        foreach (string queueName in response.Data!)
            await queueManager.PublishMessage(queueName, command.Message, connection, cancellationToken);

        CommandResponse successResponse = CommandResponse.Success();

        byte[] successPayload = CommandResponseEncoder.Encode(successResponse);

        Frame successFrame = new(FrameType.CommandResponse, successPayload);

        await connection.SendAsync(successFrame, cancellationToken);
    }
}