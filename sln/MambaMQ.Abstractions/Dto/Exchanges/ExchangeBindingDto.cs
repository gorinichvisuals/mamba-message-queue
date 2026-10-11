namespace MambaMQ.Abstractions.Dto.Exchanges;

public sealed record ExchangeBindingDto(
    string ExchangeName,
    string QueueName,
    string RoutingKey);