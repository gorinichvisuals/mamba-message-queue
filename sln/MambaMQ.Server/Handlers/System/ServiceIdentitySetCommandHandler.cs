namespace MambaMQ.Server.Handlers.System;

internal sealed class ServiceIdentitySetCommandHandler(IOptions<MambaServerOptions> options) : ICommandHandler<ServiceIdentitySetCommand>
{
    public async Task Handle(ServiceIdentitySetCommand command, IClientConnection connection, CancellationToken cancellationToken = default)
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

        Frame frame = new(FrameType.CommandResultResponse, payload);

        await connection.SendAsync(frame, cancellationToken);
    }
}