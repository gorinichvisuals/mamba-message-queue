namespace MambaMQ.Server.Handlers.Queues;

internal sealed class QueueMessagesGetCommandHandler(IQueueManager queueManager) : ICommandHandler<QueueMessagesGetCommand>
{
    public async Task Handle(QueueMessagesGetCommand command, IClientConnection connection, CancellationToken cancellationToken = default)
    {
        CommandResponse<GetMessagesResponse> response = await queueManager.GetMessagesAsync(command.QueueName, command.Skip, command.Take, cancellationToken);

        if (!response.IsSucceed)
        {
            byte[] errorPayload = CommandResponseEncoder.Encode(response);

            Frame errorFrame = new(FrameType.CommandResultResponse, errorPayload);

            await connection.SendAsync(errorFrame, cancellationToken);

            return;
        }

        byte[] payload = GetMessagesResponseEncoder.Encode(response.Data!);

        Frame frame = new(FrameType.QueueMessagesGetResponse, payload);

        await connection.SendAsync(frame, cancellationToken);
    }
}