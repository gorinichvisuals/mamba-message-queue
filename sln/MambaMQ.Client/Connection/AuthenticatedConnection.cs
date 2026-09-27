namespace MambaMQ.Client.Connection;

internal sealed class AuthenticatedConnection(
    IConnection connection, 
    MambaClientOptions options) : IConnection
{
    public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
    {
        await connection.ConnectAsync(host, port, cancellationToken);

        try
        {
            await AuthenticateAsync(cancellationToken);
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

    public ValueTask DisposeAsync() => connection.DisposeAsync();
    
    private async Task AuthenticateAsync(CancellationToken cancellationToken)
    {
        AuthenticationCommand command = new(options.Credentials.Username, options.Credentials.Password);

        byte[] payload = CommandEncoder.Encode(command);

        Frame frame = new(FrameType.Authentication, payload);

        byte[] buffer = FrameEncoder.Encode(frame);

        await connection.SendAsync(buffer, cancellationToken);

        Frame responseFrame = await FrameReader.ReadAsync(connection, options.MaxMessageSizeInBytes, cancellationToken);

        if (responseFrame.Type is not FrameType.Authentication)
            throw new InvalidDataException($"Expected authentication response, received '{responseFrame.Type}'.");

        AuthenticationResponse response = AuthenticationResponseDecoder.Decode(responseFrame.Payload.Span);

        if (!response.Success)
            throw new AuthenticationException(response.ErrorMessage ?? "Authentication failed.");
    }
}