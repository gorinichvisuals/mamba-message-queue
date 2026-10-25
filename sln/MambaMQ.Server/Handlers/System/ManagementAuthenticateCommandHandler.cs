namespace MambaMQ.Server.Handlers.System;

internal sealed class ManagementAuthenticateCommandHandler(IAuthenticationService authenticationService) : ICommandHandler<ManagementAuthenticateCommand>
{
    public async Task Handle(ManagementAuthenticateCommand command, IClientConnection connection, CancellationToken cancellationToken = default)
    {
        CommandResponse response = authenticationService.AuthenticateManagement(command.UserName, command.Password, cancellationToken);
        
        await SendResponse(connection, response, cancellationToken);

        if (response.IsSucceed)
            connection.Authenticate(ClientType.Management);
    }

    private static async Task SendResponse(IClientConnection connection, CommandResponse response, CancellationToken cancellationToken)
    {
        byte[] payload = CommandResponseEncoder.Encode(response);
        
        await connection.SendAsync(new Frame(FrameType.CommandResultResponse, payload), cancellationToken);
    }
}