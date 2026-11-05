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

    public CommandResponse AuthenticateClient(string username, string password, CancellationToken cancellationToken = default)
    {
        if (!Authenticate(username, password))
        {
            logger.LogWarning("Management authentication failed for user '{Username}'.", username);
            
            return CommandResponse.Fail(ErrorCode.AuthenticationFailed, "Authentication failed.");
        }

        logger.LogInformation("Management user '{Username}' authenticated successfully.", username);
        
        return CommandResponse.Success();
    }

    private bool Authenticate(string username, string password)
        => string.Equals(username, options.Value.Username, StringComparison.Ordinal)
            && !string.IsNullOrEmpty(PasswordHash)
            && Argon2.Verify(PasswordHash, password);
}