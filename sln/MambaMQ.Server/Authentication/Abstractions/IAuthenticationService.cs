namespace MambaMQ.Server.Authentication.Abstractions;

public interface IAuthenticationService
{
    void InitializeUserCredentials();
    AuthenticationResponse Authenticate(string username, string password, CancellationToken cancellationToken = default);
}