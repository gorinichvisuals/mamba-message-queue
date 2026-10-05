namespace MambaMQ.Protocol.Responses.Models.Queues;

public sealed class QueueModel
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsDurable { get; init; }
    public LoadBalancingAlgorithm LoadBalancingAlgorithm { get; init; }
    public Dictionary<string, QueuePermission> Permissions { get; init; } = [];
    public bool MessageRetentionEnabled { get; init; }
    public TimeSpan MessageRetentionPeriod { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public int AvailableMessageCount { get; init; }
    public int InFlightMessageCount { get; init; }
    
    public string PermissionsDisplay =>
        Permissions.Count == 0
            ? "—"
            : string.Join(
                ", ",
                Permissions.Select(x => $"{x.Key}: {x.Value}"));
}