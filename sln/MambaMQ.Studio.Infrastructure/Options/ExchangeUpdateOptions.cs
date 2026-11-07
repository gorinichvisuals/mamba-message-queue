namespace MambaMQ.Studio.Infrastructure.Options;

public sealed class ExchangeUpdateOptions
{
    public required string Name { get; init; }
    public ExchangeType Type { get; init; } = ExchangeType.Direct;
    public Dictionary<string, ExchangePermission> Permissions { get; init; } = [];
}