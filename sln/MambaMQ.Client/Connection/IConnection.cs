namespace MambaMQ.Client.Connection;

public interface IConnection : IAsyncDisposable
{    
    Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default);
    Task SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default);
    ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken = default);
}