namespace MambaMQ.Protocol.Responses.Models.Exchanges;

public sealed class ExchangeModel
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsDurable { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public ExchangeType Type { get; init; }
    public Dictionary<string, ExchangePermission> Permissions { get; init; } = [];
    public IReadOnlyList<ExchangeBindingModel> Bindings { get; init; } = [];

    public int BoundQueueCount => Bindings
        .Select(binding => binding.QueueName)
        .Distinct(StringComparer.Ordinal)
        .Count();
}