namespace MambaMQ.Studio.Infrastructure.Client;

internal sealed partial class MambaStudioClient(IConnection connection, IAuthenticateService authenticateService) : IMambaStudioClient
{
    private const int MaxMessageSizeInBytes = 1048576;
    private readonly SemaphoreSlim _requestLock = new(1, 1);

    public async Task ConnectAsync(string host, int port, string username, string password, CancellationToken cancellationToken = default)
    {
        await _requestLock.WaitAsync(cancellationToken);

        try
        {
            await connection.ConnectAsync(host, port, cancellationToken);
            await authenticateService.AuthenticateAsync(username, password, cancellationToken);
        }
        finally
        {
            _requestLock.Release();
        }
    }

    public async Task PingAsync(CancellationToken cancellationToken = default)
    {
        ConnectionPingCommand command = new();

        byte[] payload = CommandEncoder.Encode(command);

        Frame frame = new(FrameType.ConnectionPingCommand, payload);

        Frame responseFrame = await SendRequestAsync(frame, cancellationToken);

        if (responseFrame.Type is not FrameType.ConnectionPongResponse)
            throw new InvalidDataException($"Expected {FrameType.ConnectionPongResponse} frame, but received {responseFrame.Type}.");
    }

    public ValueTask DisconnectAsync(CancellationToken cancellationToken = default)
        => connection.DisposeAsync();

    private async Task<Frame> SendRequestAsync(Frame frame, CancellationToken cancellationToken)
    {
        await _requestLock.WaitAsync(cancellationToken);

        try
        {
            await connection.SendAsync(FrameEncoder.Encode(frame), cancellationToken);
            return await FrameReader.ReadAsync(connection, MaxMessageSizeInBytes, cancellationToken);
        }
        finally
        {
            _requestLock.Release();
        }
    }
}