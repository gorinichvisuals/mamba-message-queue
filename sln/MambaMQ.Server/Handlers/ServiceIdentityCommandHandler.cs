namespace MambaMQ.Server.Handlers;

internal sealed class ServiceIdentityCommandHandler(IOptions<MambaServerOptions> options) : ICommandHandler<ServiceIdentityCommand>
{
    public Task Handle(ServiceIdentityCommand command, IClientConnection connection,
        CancellationToken cancellationToken = default)
    {
        if(!options.Value.AuthorizationEnabled)
            return Task.CompletedTask;
        
        connection.IdentifyService(command.ServiceName);

        return Task.CompletedTask;
    }
}