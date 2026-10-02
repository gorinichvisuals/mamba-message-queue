namespace MambaMQ.Server.Server;

internal sealed class MambaServer(
    ICommandDispatcher dispatcher, 
    IAuthenticationService authenticationService,
    IQueueRecoveryService queueRecoveryService,
    IServerStorageService serverStorage,
    IOptions<MambaServerOptions> options,
    IMambaLogger mambaLogger)
{
    private readonly ILogger _logger = mambaLogger.Server;

    private TcpListener? _tcpListener;
    private int _activeConnections;
    
    public async Task Start(CancellationToken cancellationToken = default)
    {
        authenticationService.InitializeUserCredentials();
        
        await queueRecoveryService.RestoreQueues(cancellationToken);
        
        _logger.LogInformation("Queues recovered successfully.");
        
        _tcpListener = new TcpListener(IPAddress.Any,  options.Value.Port);
        
        _tcpListener.Start();
        
        _logger.LogInformation("MambaMQ server started on port {Port}.", options.Value.Port);
        
        Task serverTask = AcceptClientsAsync(cancellationToken);
        Task cleanupMessagesTask = RunMessagesCleanup(cancellationToken);
        
        await Task.WhenAll(serverTask, cleanupMessagesTask);
    }

    private async Task AcceptClientsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient client = await _tcpListener!.AcceptTcpClientAsync(cancellationToken);

            _logger.LogDebug("Client connection accepted from {RemoteEndPoint}.", client.Client.RemoteEndPoint);
            
            _ = HandleClient(client, cancellationToken);
        }
    }

    private async Task RunMessagesCleanup(CancellationToken cancellationToken)
    {    
        if (!options.Value.QueueStorage.MessageCleanupEnabled)
            return;
        
        using PeriodicTimer timer = new(options.Value.QueueStorage.CleanupMessageInterval);

        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            try
            {
                await serverStorage.CleanupMessages(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to cleanup messages.");
            }
        }
    }
    
    private async Task HandleClient(TcpClient client, CancellationToken cancellationToken)
    {
        await using ClientConnection connection = new(client, dispatcher, options.Value.MaxMessageSizeInBytes, mambaLogger);

        int activeConnections = Interlocked.Increment(ref _activeConnections);

        _logger.LogDebug("Client {ClientId} connected. Active connections: {ActiveConnections}.", connection.Id, activeConnections);

        try
        {
            await connection.RunAsync(cancellationToken);
        }
        finally
        {
            activeConnections = Interlocked.Decrement(ref _activeConnections);

            _logger.LogDebug("Client {ClientId} disconnected. Active connections: {ActiveConnections}.", connection.Id, activeConnections);
        }
    }
}