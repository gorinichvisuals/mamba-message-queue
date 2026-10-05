namespace MambaMQ.Server.Handlers;

internal sealed class AuthenticationCommandHandler(IAuthenticationService authenticationService) : ICommandHandler<AuthenticationCommand>
{
    public async Task Handle(AuthenticationCommand command, IClientConnection connection, CancellationToken cancellationToken = default)
    {
        CommandResponse response = authenticationService.Authenticate(command.UserName, command.Password, cancellationToken);
        
        byte[] payload = CommandResponseEncoder.Encode(response);

        Frame frame = new(FrameType.CommandResponse, payload);

        await connection.SendAsync(frame, cancellationToken);

        if (response.IsSucceed)
            connection.Authenticate(command.ClientType);
    }
}