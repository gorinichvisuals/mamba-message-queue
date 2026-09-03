namespace MambaMQ.Client.Connection;

internal sealed class TcpConnection : IConnection
{
    private TcpClient? _client;
    private NetworkStream? _stream;

    public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
    {
        _client = new TcpClient();

        await _client.ConnectAsync(host, port, cancellationToken);

        _stream = _client.GetStream();
    }

    public Task SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
        => _stream!.WriteAsync(data, cancellationToken).AsTask();

    public ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        => _stream!.ReadAsync(buffer, cancellationToken);

    public async ValueTask DisposeAsync()
    {
        if (_stream is not null)
            await _stream.DisposeAsync();

        _client?.Dispose();
    }
}