namespace MambaMQ.Server.Options;

public sealed class MambaServerOptions
{
    public int Port { get; set; }
    public int MaxMessageSizeInKilobytes { get; set; } = 1 * 1024 * 1024;
}