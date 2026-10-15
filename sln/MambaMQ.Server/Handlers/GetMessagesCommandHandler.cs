namespace MambaMQ.Server.Handlers;

internal sealed class GetMessagesCommandHandler(IQueueManager queueManager) : ICommandHandler<GetMessagesCommand>
{
    public async Task Handle(GetMessagesCommand command, IClientConnection connection, CancellationToken cancellationToken = default)
    {
        CommandResponse<GetMessagesResponse> response = await queueManager.GetMessagesAsync(command.QueueName, command.Skip, command.Take, cancellationToken);

        if (!response.IsSucceed)
        {
            byte[] errorPayload = CommandResponseEncoder.Encode(response);

            Frame errorFrame = new(FrameType.CommandResponse, errorPayload);

            await connection.SendAsync(errorFrame, cancellationToken);

            return;
        }

        byte[] payload = GetMessagesResponseEncoder.Encode(response.Data!);

        Frame frame = new(FrameType.GetMessagesResponse, payload);

        await connection.SendAsync(frame, cancellationToken);
    }
}