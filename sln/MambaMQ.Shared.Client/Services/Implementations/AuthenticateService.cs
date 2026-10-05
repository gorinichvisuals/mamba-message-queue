namespace MambaMQ.Shared.Client.Services.Implementations;

internal sealed class AuthenticateService(IConnection connection) : IAuthenticateService
{
    public async Task AuthenticateAsync(string username, string password, CancellationToken cancellationToken)
    {
        AuthenticationCommand command = new(username, password);

        byte[] payload = CommandEncoder.Encode(command);

        Frame frame = new(FrameType.Authentication, payload);

        byte[] buffer = FrameEncoder.Encode(frame);

        await connection.SendAsync(buffer, cancellationToken);

        CommandResponse response = await ReadCommandResponseAsync(cancellationToken: cancellationToken);

        if (!response.IsSucceed)
            throw new AuthenticationException(response.ErrorMessage ?? "Authentication failed.");
    }
    
    public async Task<CommandResponse> ReadCommandResponseAsync(int maxMessageSizeInBytes = 1048576, CancellationToken cancellationToken = default)
    {
        Frame responseFrame = await FrameReader.ReadAsync(connection, maxMessageSizeInBytes, cancellationToken);

        if (responseFrame.Type is not FrameType.CommandResponse)
            throw new InvalidDataException($"Expected command response, received '{responseFrame.Type}'.");

        return CommandResponseDecoder.Decode(responseFrame.Payload.Span);
    }
}