namespace MambaMQ.Server.Handlers.System;

internal sealed class ServiceAuthenticateCommandHandler(IAuthenticationService authenticationService) : ICommandHandler<ServiceAuthenticateCommand>
{
    public async Task Handle(ServiceAuthenticateCommand command, IClientConnection connection, CancellationToken cancellationToken = default)
    {
        CommandResponse response = authenticationService.AuthenticateService(command.ServerKey, cancellationToken);
        
        await SendResponse(connection, response, cancellationToken);

        if (response.IsSucceed)
            connection.Authenticate(ClientType.Service);
    }

    private static async Task SendResponse(IClientConnection connection, CommandResponse response, CancellationToken cancellationToken)
    {
        byte[] payload = CommandResponseEncoder.Encode(response);
        
        await connection.SendAsync(new Frame(FrameType.CommandResultResponse, payload), cancellationToken);
    }
}