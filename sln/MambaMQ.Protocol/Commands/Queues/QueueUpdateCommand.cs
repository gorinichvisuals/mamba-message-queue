namespace MambaMQ.Protocol.Commands.Queues;

public sealed class QueueUpdateCommand(
    Guid queueId,
    string queueName,
    bool isDurable,
    LoadBalancingAlgorithm loadBalancingAlgorithm,
    Dictionary<string, QueuePermission> permissions,
    bool messageRetentionEnabled,
    TimeSpan messageRetentionPeriod) : ICommand
{
    public FrameType Type => FrameType.QueueUpdateCommand;

    public Guid QueueId { get; } = queueId;
    public string QueueName { get; } = queueName;
    public bool IsDurable { get; } = isDurable;
    public LoadBalancingAlgorithm LoadBalancingAlgorithm { get; } = loadBalancingAlgorithm;
    public Dictionary<string, QueuePermission> Permissions { get; } = permissions;
    public bool MessageRetentionEnabled { get; } = messageRetentionEnabled;
    public TimeSpan MessageRetentionPeriod { get; } = messageRetentionPeriod;
}