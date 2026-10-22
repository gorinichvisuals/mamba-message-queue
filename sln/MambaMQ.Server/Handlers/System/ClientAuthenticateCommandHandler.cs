namespace MambaMQ.Server.Handlers.System;

internal sealed class ClientAuthenticateCommandHandler(IAuthenticationService authenticationService) : ICommandHandler<ClientAuthenticateCommand>
{
    public async Task Handle(ClientAuthenticateCommand command, IClientConnection connection, CancellationToken cancellationToken = default)
    {
        CommandResponse response = authenticationService.Authenticate(command.UserName, command.Password, cancellationToken);

        byte[] payload = CommandResponseEncoder.Encode(response);

        Frame frame = new(FrameType.CommandResultResponse, payload);

        await connection.SendAsync(frame, cancellationToken);

        if (response.IsSucceed)
            connection.Authenticate(command.ClientType);
    }
}