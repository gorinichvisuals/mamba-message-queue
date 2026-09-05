namespace MambaMQ.Server.Connections;

internal sealed class ClientConnection(
    TcpClient client, 
    ICommandDispatcher dispatcher,
    int maxMessageSizeInKilobytes) : IClientConnection, IAsyncDisposable
{
    public Guid Id { get; } = Guid.CreateVersion7();

    private NetworkStream? _stream;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    
    private readonly List<Task> _tasks = [];
    
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _stream = client.GetStream();

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Frame frame = await FrameReader.ReadFrameAsync(_stream, maxMessageSizeInKilobytes, cancellationToken);

                ICommand command = CommandDecoder.Decode(frame.Type, frame.Payload.Span);

                Task task = dispatcher.DispatchAsync(this, command, cancellationToken);

                Track(task);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (IOException)
        {
        }
        catch (Exception)
        {
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
    }
}