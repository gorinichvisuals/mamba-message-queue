namespace MambaMQ.Server.Handlers.Exchanges;

internal sealed class ExchangeMessagePublishCommandHandler(
    IExchangeManager exchangeManager,
    IQueueManager queueManager) : ICommandHandler<ExchangeMessagePublishCommand>
{
    public async Task Handle(ExchangeMessagePublishCommand command, IClientConnection connection, CancellationToken cancellationToken)
    {
        CommandResponse<IReadOnlyList<string>> response = exchangeManager.ResolveQueues(command.ExchangeName, command.RoutingKey, connection);

        if (!response.IsSucceed)
        {
            byte[] payload = CommandResponseEncoder.Encode(response);

            Frame frame = new(FrameType.CommandResultResponse, payload);

            await connection.SendAsync(frame, cancellationToken);

            return;
        }

        foreach (string queueName in response.Data!)
            await queueManager.PublishMessage(queueName, command.Message, connection, cancellationToken);

        CommandResponse successResponse = CommandResponse.Success();

        byte[] successPayload = CommandResponseEncoder.Encode(successResponse);

        Frame successFrame = new(FrameType.CommandResultResponse, successPayload);

        await connection.SendAsync(successFrame, cancellationToken);
    }
}