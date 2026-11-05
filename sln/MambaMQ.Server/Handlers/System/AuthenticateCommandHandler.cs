namespace MambaMQ.Server.Handlers.System;

internal sealed class AuthenticateCommandHandler(IAuthenticationService authenticationService) : ICommandHandler<AuthenticateCommand>
{
    public async Task Handle(AuthenticateCommand command, IClientConnection connection, CancellationToken cancellationToken = default)
    {
        CommandResponse response = authenticationService.AuthenticateClient(command.UserName, command.Password, cancellationToken);
        
        await SendResponse(connection, response, cancellationToken);

        if (response.IsSucceed)
            connection.Authenticate(command.ClientType);
    }

    private static async Task SendResponse(IClientConnection connection, CommandResponse response, CancellationToken cancellationToken)
    {
        byte[] payload = CommandResponseEncoder.Encode(response);
        
        await connection.SendAsync(new Frame(FrameType.CommandResultResponse, payload), cancellationToken);
    }
}