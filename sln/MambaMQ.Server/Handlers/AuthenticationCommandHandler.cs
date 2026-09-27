namespace MambaMQ.Server.Handlers;

internal sealed class AuthenticationCommandHandler(IAuthenticationService authenticationService) : ICommandHandler<AuthenticationCommand>
{
    public async Task Handle(AuthenticationCommand command, IClientConnection connection, CancellationToken cancellationToken = default)
    {
        AuthenticationResponse response = authenticationService.Authenticate(command.UserName, command.Password, cancellationToken);
        
        byte[] payload = AuthenticationResponseEncoder.Encode(response);

        Frame frame = new(FrameType.Authentication, payload);

        await connection.SendAsync(frame, cancellationToken);

        if (response.Success)
            connection.Authenticate();
    }
}