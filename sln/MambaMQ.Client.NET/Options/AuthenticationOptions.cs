namespace MambaMQ.Client.NET.Options;

/// <summary>Authentication credentials used by the client.</summary>
public sealed class AuthenticationOptions
{
    /// <summary>The server key used to authenticate this service client.</summary>
    public string ServerKey { get; set; } = string.Empty;
}