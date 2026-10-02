namespace MambaMQ.Server.Options;

public sealed class MambaServerOptions
{
    public int Port { get; init; } = 24;
    public int MaxMessageSizeInBytes { get; init; } = 1048576;
    public bool AuthorizationEnabled { get; init; } = false;
    
    public QueueStorageOptions QueueStorage { get; init; } = new();
    public ServerLoggingOptions ServerLogging { get; init; } = new();
    public AuthenticationOptions Authentication { get; init; } = new();
}