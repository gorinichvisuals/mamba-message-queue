namespace MambaMQ.Server.Server;

internal sealed class MambaServer(ICommandDispatcher dispatcher, IOptions<MambaServerOptions> options)
{
    private TcpListener? _tcpListener;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _tcpListener = new TcpListener(IPAddress.Any,  options.Value.Port);
        
        _tcpListener.Start();
        
        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient client = await _tcpListener.AcceptTcpClientAsync(cancellationToken);

            _ = HandleClientAsync(client, cancellationToken);
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        await using ClientConnection connection = new ClientConnection(client, dispatcher, options.Value);
        
        await connection.RunAsync(cancellationToken);
    }
}