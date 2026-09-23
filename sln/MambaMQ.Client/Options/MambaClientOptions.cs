namespace MambaMQ.Client.Options;

public sealed class MambaClientOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 24;
    public int MaxMessageSizeInBytes { get; set; } = 1 * 1024 * 1024;
}