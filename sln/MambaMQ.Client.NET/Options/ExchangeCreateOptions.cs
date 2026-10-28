namespace MambaMQ.Client.NET.Options;

/// <summary>Specifies the settings used to create or update an exchange.</summary>
public class ExchangeCreateOptions
{
    /// <summary>The required exchange name.</summary>
    public required string Name { get; set; }

    /// <summary>Whether the exchange survives server restarts; defaults to <see langword="true"/>.</summary>
    public bool IsDurable { get; set; } = true;

    /// <summary>The exchange routing type; defaults to <see cref="ExchangeType.Direct"/>.</summary>
    public ExchangeType Type { get; set; } = ExchangeType.Direct;

    /// <summary>The permissions assigned to users for this exchange.</summary>
    public Dictionary<string, ExchangePermission> Permissions { get; set; } = [];
}