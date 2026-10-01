namespace MambaMQ.Authentication.Services;

public interface IAuthenticationService
{
    void InitializeUserCredentials();
    AuthenticationResponse Authenticate(string username, string password, CancellationToken cancellationToken = default);
}