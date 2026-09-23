namespace MambaMQ.Server.Server;

internal sealed class MambaServer(
    ICommandDispatcher dispatcher, 
    IQueueRecoveryService queueRecoveryService,
    IQueueStorageService queueStorage,
    IOptions<MambaServerOptions> options)
{
    private TcpListener? _tcpListener;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await queueRecoveryService.RecoverAsync(cancellationToken);
        
        _tcpListener = new TcpListener(IPAddress.Any,  options.Value.Port);
        
        _tcpListener.Start();
        
        Task serverTask = AcceptClientsAsync(cancellationToken);
        Task cleanupTask = RunCleanupAsync(cancellationToken);

        await Task.WhenAll(serverTask, cleanupTask);
    }

    private async Task AcceptClientsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient client = await _tcpListener!.AcceptTcpClientAsync(cancellationToken);

            _ = HandleClientAsync(client, cancellationToken);
        }
    }

    private async Task RunCleanupAsync(CancellationToken cancellationToken)
    {
        using PeriodicTimer timer = new(options.Value.Storage.CleanupInterval);

        while (await timer.WaitForNextTickAsync(cancellationToken))
            await queueStorage.Cleanup(cancellationToken);
    }
    
    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        await using ClientConnection connection = new ClientConnection(client, dispatcher, options.Value.MaxMessageSizeInBytes);
        
        await connection.RunAsync(cancellationToken);
    }
}