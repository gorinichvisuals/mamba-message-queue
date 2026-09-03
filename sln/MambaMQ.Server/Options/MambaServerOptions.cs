namespace MambaMQ.Server.Options;

public sealed class MambaServerOptions
{
    public int Port { get; set; }
    public TimeSpan MessageTtl { get; set; } = TimeSpan.FromMinutes(10);
    public TimeSpan ExpirationCheckInterval { get; set; } = TimeSpan.FromMinutes(1);
}