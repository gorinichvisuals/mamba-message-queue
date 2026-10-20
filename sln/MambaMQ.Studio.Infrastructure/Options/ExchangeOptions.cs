namespace MambaMQ.Studio.Infrastructure.Options;

public sealed class ExchangeOptions
{
    public required string Name { get; init; }
    public bool IsDurable { get; init; } = true;
    public ExchangeType Type { get; init; } = ExchangeType.Direct;
    public Dictionary<string, ExchangePermission> Permissions { get; init; } = [];
}
