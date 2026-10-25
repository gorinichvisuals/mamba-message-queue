namespace MambaMQ.Studio.Infrastructure.Services.Implementations;

internal sealed class AuthenticateService(IConnection connection) : IAuthenticateService
{
    private const int MaxMessageSizeInBytes = 1048576;

    public async Task AuthenticateAsync(string username, string password, CancellationToken cancellationToken)
    {
        ManagementAuthenticateCommand command = new(username, password);

        byte[] payload = CommandEncoder.Encode(command);

        Frame frame = new(FrameType.ManagementAuthenticateCommand, payload);

        byte[] buffer = FrameEncoder.Encode(frame);

        await connection.SendAsync(buffer, cancellationToken);

        CommandResponse response = await ReadCommandResponseAsync(cancellationToken: cancellationToken);

        if (!response.IsSucceed)
            throw new AuthenticationException(response.ErrorMessage ?? "Authentication failed.");
    }

    private async Task<CommandResponse> ReadCommandResponseAsync(CancellationToken cancellationToken = default)
    {
        Frame responseFrame = await FrameReader.ReadAsync(connection, MaxMessageSizeInBytes, cancellationToken);

        return responseFrame.Type is not FrameType.CommandResultResponse 
            ? throw new InvalidDataException($"Expected command response, received '{responseFrame.Type}'.") 
            : CommandResponseDecoder.Decode(responseFrame.Payload.Span);
    }
}