namespace MambaMQ.Client.NET.Connection;

internal sealed class AuthenticatedConnection(IConnection connection, MambaClientOptions options) : IConnection
{
    public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
    {
        await connection.ConnectAsync(host, port, cancellationToken);

        try
        {
            await AuthenticateAsync(cancellationToken);

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

    private async Task AuthenticateAsync(CancellationToken cancellationToken)
    {
        AuthenticationCommand command = new(options.Credentials.Username, options.Credentials.Password);

        byte[] payload = CommandEncoder.Encode(command);

        Frame frame = new(FrameType.Authentication, payload);

        byte[] buffer = FrameEncoder.Encode(frame);

        await connection.SendAsync(buffer, cancellationToken);

        CommandResponse response = await ReadCommandResponseAsync(cancellationToken);

        if (!response.IsSucceed)
            throw new AuthenticationException(response.ErrorMessage ?? "Authentication failed.");
    }

    private async Task IdentifyServiceAsync(CancellationToken cancellationToken)
    {
        ServiceIdentityCommand command = new(options.ServiceName!);

        byte[] payload = CommandEncoder.Encode(command);

        Frame frame = new(FrameType.ServiceIdentity, payload);

        byte[] buffer = FrameEncoder.Encode(frame);

        await connection.SendAsync(buffer, cancellationToken);

        CommandResponse response = await ReadCommandResponseAsync(cancellationToken);

        if (!response.IsSucceed)
            throw new InvalidOperationException(response.ErrorMessage ?? "Service identification failed.");
    }

    private async Task<CommandResponse> ReadCommandResponseAsync(CancellationToken cancellationToken)
    {
        Frame responseFrame = await FrameReader.ReadAsync(connection, options.MaxMessageSizeInBytes, cancellationToken);

        if (responseFrame.Type is not FrameType.CommandResponse)
            throw new InvalidDataException($"Expected command response, received '{responseFrame.Type}'.");

        return CommandResponseDecoder.Decode(responseFrame.Payload.Span);
    }
}