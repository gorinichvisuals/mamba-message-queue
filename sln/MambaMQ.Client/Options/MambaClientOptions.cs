namespace MambaMQ.Client.Options;

public sealed class MambaClientOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 8024;
    public int MaxMessageSizeInKilobytes { get; set; } = 1 * 1024 * 1024;
}