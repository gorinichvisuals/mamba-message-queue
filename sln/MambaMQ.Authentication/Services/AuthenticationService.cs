namespace MambaMQ.Authentication.Services;

internal sealed class AuthenticationService(
    ILogger<AuthenticationService> logger,
    IOptions<AuthenticationOptions> options) : IAuthenticationService
{
    private string PasswordHash { get; set; } = string.Empty;

    public void InitializeUserCredentials()
    {
        PasswordHash = Argon2.Hash(options.Value.Password);
        logger.LogInformation("Management credentials initialized for user '{Username}'.", options.Value.Username);
    }

    public CommandResponse AuthenticateService(string serverKey, CancellationToken cancellationToken = default)
    {
        if (!AuthenticateServiceKey(serverKey))
        {
            logger.LogWarning("Service authentication failed.");
            
            return CommandResponse.Fail(ErrorCode.AuthenticationFailed, "Authentication failed.");
        }

        logger.LogInformation("Service client authenticated.");
        
        return CommandResponse.Success();
    }

    public CommandResponse AuthenticateManagement(string username, string password, CancellationToken cancellationToken = default)
    {
        if (!AuthenticateManagementUser(username, password))
        {
            logger.LogWarning("Management authentication failed for user '{Username}'.", username);
            
            return CommandResponse.Fail(ErrorCode.AuthenticationFailed, "Authentication failed.");
        }

        logger.LogInformation("Management user '{Username}' authenticated successfully.", username);
        
        return CommandResponse.Success();
    }

    private bool AuthenticateServiceKey(string serverKey)
    {
        string configuredKey = options.Value.ServiceKey;
        
        return !string.IsNullOrEmpty(configuredKey) && string.Equals(configuredKey, serverKey, StringComparison.Ordinal);
    }

    private bool AuthenticateManagementUser(string username, string password)
        => string.Equals(username, options.Value.Username, StringComparison.Ordinal)
            && !string.IsNullOrEmpty(PasswordHash)
            && Argon2.Verify(PasswordHash, password);
}