namespace MambaMQ.Client.NET.Options;

/// <summary>Specifies the settings that can be changed on an existing exchange.</summary>
public sealed class ExchangeUpdateOptions
{
    /// <summary>The required exchange name.</summary>
    public required string Name { get; init; }

    /// <summary>The exchange routing type.</summary>
    public ExchangeType Type { get; init; } = ExchangeType.Direct;

    /// <summary>The permissions assigned to users for this exchange.</summary>
    public Dictionary<string, ExchangePermission> Permissions { get; init; } = [];
}