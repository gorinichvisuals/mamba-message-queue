namespace MambaMQ.Server.Connections;

internal sealed class ClientConnection(
    TcpClient client, 
    ICommandDispatcher dispatcher,
    int maxMessageSizeInBytes,
    IMambaLogger mambaLogger) : IClientConnection, IAsyncDisposable
{
    public Guid Id { get; } = Guid.CreateVersion7();
    private bool _isAuthenticated;
    public string ServiceName { get; private set; } = string.Empty;
    
    private NetworkStream? _stream;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    
    private readonly List<Task> _tasks = [];
    
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _stream = client.GetStream();

        mambaLogger.Server.LogInformation("Client {ClientId} connected.", Id);
        
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Frame frame = await FrameReader.ReadFrameAsync(_stream, maxMessageSizeInBytes, cancellationToken);

                ICommand command = CommandDecoder.Decode(frame.Type, frame.Payload.Span);

                if (!_isAuthenticated)
                {
                    if (command is not AuthenticationCommand)
                    {
                        mambaLogger.Server.LogWarning("Client {ClientId} attempted to execute a command before authentication.", Id);

                        break;
                    }

                    await dispatcher.DispatchAsync(this, command, cancellationToken);

                    continue;
                }

                Task task = dispatcher.DispatchAsync(this, command, cancellationToken);

                Track(task);
            }
        }
        catch (OperationCanceledException ) when (cancellationToken.IsCancellationRequested)
        {        
        }
        catch (IOException)
        { 
        }
        catch (Exception exception)
        {             
            mambaLogger.Server.LogError(exception, "Unhandled error in client connection {ClientId}.", Id);
        }
        finally
        {
            await StopAsync();
            await DisposeAsync();
        }
    }
    
    public async Task SendAsync(Frame frame, CancellationToken cancellationToken = default)
    {
        if (_stream is null)
            throw new InvalidOperationException("Connection has not been started.");

        byte[] buffer = FrameEncoder.Encode(frame);
        
        await _writeLock.WaitAsync(cancellationToken);

        try
        {
            await _stream.WriteAsync(buffer, cancellationToken);
        }
        finally
        {
            _writeLock.Release();
        }
    }
    
    private void Track(Task task)
    {
        lock (_tasks)
            _tasks.Add(task);

        _ = task.ContinueWith(
            completedTask =>
            {
                lock (_tasks)
                    _tasks.Remove(completedTask);
            },
            CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }
    
    private async Task StopAsync()
    {
        Task[] tasks;

        lock (_tasks)
            tasks = _tasks.ToArray();

        await Task.WhenAll(tasks);
    }
    
    public async ValueTask DisposeAsync()
    {
        if(_stream is not null)
            await _stream.DisposeAsync();
        
        client.Dispose();
        
        mambaLogger.Server.LogDebug("Client {ClientId} connection closed.", Id);
    }
    
    public void Authenticate()
    {
        _isAuthenticated = true;

        mambaLogger.Server.LogInformation("Client {ClientId} authenticated.", Id);
    }

    public void IdentifyService(string serviceName)
    {
        ServiceName = serviceName;

        mambaLogger.Server.LogInformation("Service '{ServiceName}' identified on client {ClientId}.", serviceName, Id);
    }
}