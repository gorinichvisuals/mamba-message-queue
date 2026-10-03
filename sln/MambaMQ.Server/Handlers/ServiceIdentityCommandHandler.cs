namespace MambaMQ.Server.Handlers;

internal sealed class ServiceIdentityCommandHandler(IOptions<MambaServerOptions> options) : ICommandHandler<ServiceIdentityCommand>
{
    public async Task Handle(ServiceIdentityCommand command, IClientConnection connection, CancellationToken cancellationToken = default)
    {
        if (!options.Value.AuthorizationEnabled)
        {
            await SendResponse(connection, CommandResponse.Success(), cancellationToken);

            return;
        }

        if (string.IsNullOrWhiteSpace(command.ServiceName))
        {
            await SendResponse(connection, CommandResponse.Fail(ErrorCode.InvalidArgument, "Service name cannot be empty."), cancellationToken);

            return;
        }

        connection.IdentifyService(command.ServiceName);

        await SendResponse(connection, CommandResponse.Success(), cancellationToken);
    }

    private static async Task SendResponse(IClientConnection connection, CommandResponse response, CancellationToken cancellationToken)
    {
        byte[] payload = CommandResponseEncoder.Encode(response);

        Frame frame = new(FrameType.CommandResponse, payload);

        await connection.SendAsync(frame, cancellationToken);
    }
}