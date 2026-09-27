namespace MambaMQ.Server.Authentication.Implementations;

internal sealed class AuthenticationService(
    ILogger<AuthenticationService> logger,
    IOptions<AuthenticationOptions> options) : IAuthenticationService
{
    private string _username = string.Empty;
    private string PasswordHash { get; set; } = string.Empty;

    public void InitializeUserCredentials()
    {
        _username = options.Value.Username;
        PasswordHash = Argon2.Hash(options.Value.Password);

        logger.LogInformation("User credentials initialized for user '{Username}'.", _username);
    }

    public AuthenticationResponse Authenticate(string username, string password, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(username, _username, StringComparison.Ordinal))
        {
            logger.LogWarning( "Authentication failed for user '{Username}'.", username); 
            
            return new AuthenticationResponse(false, "Authentication failed.");
        }

        bool passwordValid = Argon2.Verify(PasswordHash, password);
        
        if (!passwordValid)
        {
            logger.LogWarning( "Authentication failed for user '{Username}'.", username); 
            
            return new AuthenticationResponse(false, "Authentication failed.");
        } 
        
        logger.LogInformation( "User '{Username}' authenticated successfully.", username); 
        
        return new AuthenticationResponse(true);
    }
}