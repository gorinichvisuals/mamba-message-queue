namespace MambaMQ.Server.Options;

public sealed class AuthenticationOptions
{
    public string Username { get; init; } = "admin";
    public string Password { get; init; } = "admin";
}