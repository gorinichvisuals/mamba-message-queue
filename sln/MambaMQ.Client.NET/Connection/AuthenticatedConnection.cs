namespace MambaMQ.Client.NET.Connection;

internal sealed class AuthenticatedConnection(
    IConnection connection,
    MambaClientOptions options) : IConnection
{
    private const ClientType ClientTypeValue = ClientType.Service;

    public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
    {
        await connection.ConnectAsync(host, port, cancellationToken);

        try
        {
            await AuthenticateAsync(options.Credentials.ServerKey, cancellationToken);

            if (!string.IsNullOrWhiteSpace(options.ServiceName))
                await IdentifyServiceAsync(cancellationToken);
        }
        catch
        {
            await connection.DisposeAsync();

            throw;
        }
    }

    public Task SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
        => connection.SendAsync(data, cancellationToken);

    public ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        => connection.ReceiveAsync(buffer, cancellationToken);

    public ValueTask DisposeAsync()
        => connection.DisposeAsync();

    private async Task AuthenticateAsync(string serverKey, CancellationToken cancellationToken)
    {
        ServiceAuthenticateCommand command = new(serverKey);

        byte[] payload = CommandEncoder.Encode(command);

        Frame frame = new(FrameType.ServiceAuthenticateCommand, payload);

        byte[] buffer = FrameEncoder.Encode(frame);

        await connection.SendAsync(buffer, cancellationToken);

        CommandResponse response = await ReadCommandResponseAsync(cancellationToken: cancellationToken);

        if (!response.IsSucceed)
            throw new AuthenticationException(response.ErrorMessage ?? "Authentication failed.");
    }

    private async Task IdentifyServiceAsync(CancellationToken cancellationToken)
    {
        ServiceIdentitySetCommand command = new(options.ServiceName!);

        byte[] payload = CommandEncoder.Encode(command);

        Frame frame = new(FrameType.ServiceIdentitySetCommand, payload);

        byte[] buffer = FrameEncoder.Encode(frame);

        await connection.SendAsync(buffer, cancellationToken);

        CommandResponse response = await ReadCommandResponseAsync(options.MaxMessageSizeInBytes, cancellationToken);

        if (!response.IsSucceed)
            throw new InvalidOperationException(response.ErrorMessage ?? "Service identification failed.");
    }

    private async Task<CommandResponse> ReadCommandResponseAsync(int maxMessageSizeInBytes = 1048576, CancellationToken cancellationToken = default)
    {
        Frame responseFrame = await FrameReader.ReadAsync(connection, maxMessageSizeInBytes, cancellationToken);

        return responseFrame.Type is not FrameType.CommandResultResponse
            ? throw new InvalidDataException($"Expected command response, received '{responseFrame.Type}'.")
            : CommandResponseDecoder.Decode(responseFrame.Payload.Span);
    }
}