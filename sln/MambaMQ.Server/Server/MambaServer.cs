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
        Task cleanupMessagesTask = RunMessagesCleanupAsync(cancellationToken);
        Task cleanupLogsTask = RunLogsCleanupAsync(cancellationToken);
        
        await Task.WhenAll(serverTask, cleanupMessagesTask, cleanupLogsTask);
    }

    private async Task AcceptClientsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient client = await _tcpListener!.AcceptTcpClientAsync(cancellationToken);

            _ = HandleClientAsync(client, cancellationToken);
        }
    }

    private async Task RunMessagesCleanupAsync(CancellationToken cancellationToken)
    {
        using PeriodicTimer timer = new(options.Value.Storage.CleanupMessagesInterval);

        while (await timer.WaitForNextTickAsync(cancellationToken))
            await queueStorage.CleanupMessages(cancellationToken);
    }

    private async Task RunLogsCleanupAsync(CancellationToken cancellationToken)
    {
        using PeriodicTimer timer = new(options.Value.Storage.CleanupLogsInterval);
        
        while (await timer.WaitForNextTickAsync(cancellationToken))
            await queueStorage.CleanupLogs(cancellationToken);
    }
    
    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        await using ClientConnection connection = new ClientConnection(client, dispatcher, options.Value.MaxMessageSizeInBytes);
        
        await connection.RunAsync(cancellationToken);
    }
}