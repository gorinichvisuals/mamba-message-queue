namespace MambaMQ.Studio.Client.NET.Client;

public interface IMambaStudioClient
{
    Task ConnectAsync(string host, int port, string username, string password, CancellationToken cancellationToken = default);
    ValueTask DisconnectAsync(CancellationToken cancellationToken = default);
}