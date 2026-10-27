namespace MambaMQ.Client.NET.Options;

public class ExchangeCreateOptions
{
    public required string Name { get; set; }
    public bool IsDurable { get; set; } = true;
    public ExchangeType Type { get; set; } = ExchangeType.Direct;
    public Dictionary<string, ExchangePermission> Permissions { get; set; } = [];
}