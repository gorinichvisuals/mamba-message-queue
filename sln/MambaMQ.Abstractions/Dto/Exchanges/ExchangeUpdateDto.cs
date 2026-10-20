namespace MambaMQ.Abstractions.Dto.Exchanges;

public sealed record ExchangeUpdateDto(
    Guid ExchangeId,
    string Name,
    bool IsDurable,
    ExchangeType Type,
    Dictionary<string, ExchangePermission> Permissions);
