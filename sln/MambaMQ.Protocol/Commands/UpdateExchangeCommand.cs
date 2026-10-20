namespace MambaMQ.Protocol.Commands;

public sealed class UpdateExchangeCommand(
    Guid exchangeId,
    string exchangeName,
    bool isDurable,
    ExchangeType exchangeType,
    Dictionary<string, ExchangePermission> permissions) : ICommand
{
    public FrameType Type => FrameType.UpdateExchangeCommand;
    public Guid ExchangeId { get; } = exchangeId;
    public string ExchangeName { get; } = exchangeName;
    public bool IsDurable { get; } = isDurable;
    public ExchangeType ExchangeType { get; } = exchangeType;
    public Dictionary<string, ExchangePermission> Permissions { get; } = permissions;
}