namespace MambaMQ.Authentication.Services;

public interface IAuthenticationService
{
    void InitializeUserCredentials();
    CommandResponse AuthenticateService(string serverKey, CancellationToken cancellationToken = default);
    CommandResponse AuthenticateManagement(string username, string password, CancellationToken cancellationToken = default);
}