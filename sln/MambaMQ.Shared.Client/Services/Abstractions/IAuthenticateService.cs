namespace MambaMQ.Shared.Client.Services.Abstractions;

public interface IAuthenticateService
{
    Task AuthenticateAsync(string username, string password, CancellationToken cancellationToken);
    Task<CommandResponse> ReadCommandResponseAsync(int maxMessageSizeInBytes = 1048576, CancellationToken cancellationToken = default);
}