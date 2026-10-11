namespace MambaMQ.Abstractions.Dto.Exchanges;

public sealed record ExchangeCreateDto(
    string Name, 
    bool IsDurable, 
    ExchangeType Type,
    Dictionary<string, ExchangePermission> Permissions);