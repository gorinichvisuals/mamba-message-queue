namespace MambaMQ.Client.Options;

public sealed class MambaClientOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public TimeSpan MessageTtl { get; set; } = TimeSpan.FromMinutes(10);
}