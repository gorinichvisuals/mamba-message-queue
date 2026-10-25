namespace MambaMQ.Studio.Infrastructure.Services.Abstractions;

public interface IAuthenticateService
{
    Task AuthenticateAsync(string username, string password, CancellationToken cancellationToken);
}