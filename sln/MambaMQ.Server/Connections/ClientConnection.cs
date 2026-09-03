namespace MambaMQ.Server.Connections;

internal sealed class ClientConnection(
    TcpClient client, 
    ICommandDispatcher dispatcher,
    MambaServerOptions options) : IClientConnection, IAsyncDisposable
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
                Frame frame = await ReadFrameAsync(cancellationToken);
                ICommand command = CommandDecoder.Decode(frame.Type, frame.Payload.Span, options.MessageTtl);
                
                Task task = dispatcher.DispatchAsync(this, command, cancellationToken);
                
                await task;
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
    
    private async Task<Frame> ReadFrameAsync(CancellationToken cancellationToken)
    {
        byte[] header = new byte[FrameConstants.HeaderSize];

        await ReadExactlyAsync(header, cancellationToken);

        int payloadLength = BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(FrameConstants.PayloadLengthOffset, FrameConstants.PayloadLengthSize));

        byte[] buffer = new byte[FrameConstants.HeaderSize + payloadLength];

        header.CopyTo(buffer, 0);

        if (payloadLength > 0)
            await ReadExactlyAsync(buffer.AsMemory(FrameConstants.HeaderSize, payloadLength), cancellationToken);

        return FrameDecoder.Decode(buffer);
    }

    private void Track(Task task)
    {
        lock (_tasks)
            _tasks.Add(task);
    }
    
    private async Task ReadExactlyAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        while (!buffer.IsEmpty)
        {
            int bytesRead = await _stream!.ReadAsync(buffer, cancellationToken);

            if (bytesRead is 0)
                throw new IOException("Client disconnected.");

            buffer = buffer[bytesRead..];
        }
    }

    public async ValueTask DisposeAsync()
    {
        if(_stream is not null)
            await _stream.DisposeAsync();
        
        client.Dispose();
    }
    
    private async Task StopAsync()
    {
        Task[] tasks;

        lock (_tasks)
            tasks = _tasks.ToArray();

        await Task.WhenAll(tasks);
    }
}