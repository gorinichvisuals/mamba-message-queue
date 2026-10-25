namespace MambaMQ.Studio.Infrastructure.Client.Factories;

using MambaMQ.Studio.Infrastructure.Client;

internal sealed class MambaStudioClientFactory : IMambaStudioClientFactory
{
    public IMambaStudioClient Create()
    {
        IConnection connection = new TcpConnection();
        IAuthenticateService authenticateService = new AuthenticateService(connection);
        return new MambaStudioClient(connection, authenticateService);
    }
}