namespace MambaMQ.Client.NET.Options;

/// <summary>Authentication credentials used by the client.</summary>
public sealed class AuthenticationOptions
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}