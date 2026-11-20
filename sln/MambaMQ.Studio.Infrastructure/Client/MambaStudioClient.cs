namespace MambaMQ.Studio.Infrastructure.Client;

internal sealed partial class MambaStudioClient(IConnection connection, IAuthenticateService authenticateService) : IMambaStudioClient
{
    private const int MaxMessageSizeInBytes = 1048576;
    private readonly SemaphoreSlim _requestLock = new(1, 1);
    private IReadOnlySet<FrameType> _supportedCommands = ProtocolCapabilities.LegacySupportedCommands;
    public bool IsServerOutdated { get; private set; }
    public event EventHandler? CompatibilityChanged;

    public bool SupportsCommand(FrameType commandType)
        => Volatile.Read(ref _supportedCommands).Contains(commandType);

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

        await PingAsync(cancellationToken);
    }

    public async Task PingAsync(CancellationToken cancellationToken = default)
    {
        ConnectionPingCommand command = new();

        byte[] payload = CommandEncoder.Encode(command);

        Frame frame = new(FrameType.ConnectionPingCommand, payload);

        Frame responseFrame = await SendRequestAsync(frame, cancellationToken);

        if (responseFrame.Type is not FrameType.ConnectionPongResponse)
            throw new InvalidDataException($"Expected {FrameType.ConnectionPongResponse} frame, but received {responseFrame.Type}.");

        if (ProtocolCapabilities.TryDecode(responseFrame.Payload.Span, out IReadOnlySet<FrameType> supportedCommands))
        {
            _supportedCommands = supportedCommands;
            SetServerOutdated(false);
        }
        else
        {
            _supportedCommands = ProtocolCapabilities.LegacySupportedCommands;
            SetServerOutdated(true);
        }
    }

    public ValueTask DisconnectAsync(CancellationToken cancellationToken = default)
        => connection.DisposeAsync();

    private async Task<Frame> SendRequestAsync(Frame frame, CancellationToken cancellationToken)
    {
        if (ProtocolCapabilities.IsCommand(frame.Type) && !SupportsCommand(frame.Type))
            throw new NotSupportedException($"The connected server does not support command '{frame.Type}'.");

        await _requestLock.WaitAsync(cancellationToken);

        try
        {
            await connection.SendAsync(FrameEncoder.Encode(frame), cancellationToken);
            Frame response = await FrameReader.ReadAsync(connection, MaxMessageSizeInBytes, cancellationToken);

            if (response.Type is FrameType.CommandResultResponse &&
                response.Payload.Length >= 2 &&
                response.Payload.Span[0] is 0 &&
                response.Payload.Span[1] == (byte)ErrorCode.UnsupportedCommand)
            {
                RemoveSupportedCommand(frame.Type);
                throw new NotSupportedException($"The connected server does not support command '{frame.Type}'.");
            }

            return response;
        }
        finally
        {
            _requestLock.Release();
        }
    }

    private void RemoveSupportedCommand(FrameType commandType)
    {
        while (true)
        {
            IReadOnlySet<FrameType> current = Volatile.Read(ref _supportedCommands);
            HashSet<FrameType> updated = current.ToHashSet();
            updated.Remove(commandType);

            if (ReferenceEquals(Interlocked.CompareExchange(ref _supportedCommands, updated, current), current))
            {
                SetServerOutdated(true);
                return;
            }
        }
    }

    private void SetServerOutdated(bool value)
    {
        if (IsServerOutdated == value)
            return;

        IsServerOutdated = value;
        CompatibilityChanged?.Invoke(this, EventArgs.Empty);
    }
}