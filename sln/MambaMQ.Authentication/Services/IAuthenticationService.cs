namespace MambaMQ.Authentication.Services;

public interface IAuthenticationService
{
    void InitializeUserCredentials();
    CommandResponse Authenticate(string username, string password, CancellationToken cancellationToken = default);
}