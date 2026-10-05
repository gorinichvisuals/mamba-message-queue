namespace MambaMQ.Studio.Client.NET.Client;

internal sealed class MambaStudioClient(
    IConnection connection,
    IAuthenticateService authenticateService) : IMambaStudioClient
{
    public async Task ConnectAsync(string host, int port, string username, string password, CancellationToken cancellationToken = default)
    {
        await connection.ConnectAsync(host, port, cancellationToken);

        await authenticateService.AuthenticateAsync(username, password, cancellationToken);
    }

    public async ValueTask DisconnectAsync(CancellationToken cancellationToken = default)
        => await connection.DisposeAsync();
}