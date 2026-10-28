namespace MambaMQ.Client.NET.Options;

/// <summary>Connection and protocol settings for a MambaMQ client.</summary>
public sealed class MambaClientOptions
{
    /// <summary>The name identifying this client service to the server.</summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>The server host name or IP address.</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>The server TCP port; defaults to 24.</summary>
    public int Port { get; set; } = 24;

    /// <summary>The maximum accepted frame size in bytes; defaults to 1 MiB.</summary>
    public int MaxMessageSizeInBytes { get; set; } = 1 * 1024 * 1024;

    /// <summary>The credentials used to authenticate with the server.</summary>
    public AuthenticationOptions Credentials { get; set; } = new();
}