namespace MambaMQ.Protocol.Commands;

public sealed class CreateExchangeCommand(
    string exchangeName,
    bool isDurable,
    ExchangeType type,
    Dictionary<string, ExchangePermission> permissions) : ICommand
{
    public FrameType Type => FrameType.CreateExchange;

    public string ExchangeName { get; } = exchangeName;
    public bool IsDurable { get; } = isDurable;
    public ExchangeType ExchangeType { get; } = type;
    public Dictionary<string, ExchangePermission> Permissions { get; } = permissions;
}