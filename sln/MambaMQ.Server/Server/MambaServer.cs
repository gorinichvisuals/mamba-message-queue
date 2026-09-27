namespace MambaMQ.Server.Server;

internal sealed class MambaServer(
    ICommandDispatcher dispatcher, 
    IQueueRecoveryService queueRecoveryService,
    IServerStorageService serverStorage,
    IOptions<MambaServerOptions> options,
    ILogger<MambaServer> logger,
    ILoggerFactory loggerFactory)
{
    private TcpListener? _tcpListener;

    public async Task Start(CancellationToken cancellationToken = default)
    {
        await queueRecoveryService.RestoreQueues(cancellationToken);
        
        logger.LogInformation("Queues recovered successfully.");
        
        _tcpListener = new TcpListener(IPAddress.Any,  options.Value.Port);
        
        _tcpListener.Start();
        
        logger.LogInformation("MambaMQ server started on port {Port}.", options.Value.Port);
        
        Task serverTask = AcceptClientsAsync(cancellationToken);
        Task cleanupMessagesTask = RunMessagesCleanup(cancellationToken);
        Task cleanupLogsTask = RunQueueLogsCleanup(cancellationToken);
        Task cleanupServerLogsTask = RunServerLogsCleanup(cancellationToken);
        
        await Task.WhenAll(serverTask, cleanupMessagesTask, cleanupLogsTask, cleanupServerLogsTask);
    }

    private async Task AcceptClientsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient client = await _tcpListener!.AcceptTcpClientAsync(cancellationToken);

            logger.LogDebug("Client connection accepted from {RemoteEndPoint}.", client.Client.RemoteEndPoint);
            
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
                logger.LogError(exception, "Failed to cleanup messages.");
            }
        }
    }

    private async Task RunQueueLogsCleanup(CancellationToken cancellationToken)
    {
        if(!options.Value.QueueStorage.LogCleanupEnabled) 
            return;
        
        using PeriodicTimer timer = new(options.Value.QueueStorage.CleanupLogInterval);
        
        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            try
            {
                await serverStorage.CleanupQueueLogs(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to cleanup queue logs.");
            }
        }
    }
    
    private async Task RunServerLogsCleanup(CancellationToken cancellationToken)
    {
        if(!options.Value.ServerLogging.CleanupLogEnabled)
            return;
        
        using PeriodicTimer timer = new(options.Value.ServerLogging.CleanupInterval);
        
        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            try
            {
                await serverStorage.CleanupServerLogs(
                    options.Value.ServerLogging.RetentionPeriod,
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to cleanup server logs.");
            }
        }
    }
    
    private async Task HandleClient(TcpClient client, CancellationToken cancellationToken)
    {
        ILogger<ClientConnection> clientLogger = loggerFactory.CreateLogger<ClientConnection>();
        
        await using ClientConnection connection = new ClientConnection(client, dispatcher, options.Value.MaxMessageSizeInBytes, clientLogger);
        
        await connection.RunAsync(cancellationToken);
    }
}