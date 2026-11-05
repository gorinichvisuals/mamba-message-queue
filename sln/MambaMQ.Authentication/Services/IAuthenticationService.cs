namespace MambaMQ.Authentication.Services;

public interface IAuthenticationService
{
    void InitializeUserCredentials();
    CommandResponse AuthenticateClient(string username, string password, CancellationToken cancellationToken = default);
}