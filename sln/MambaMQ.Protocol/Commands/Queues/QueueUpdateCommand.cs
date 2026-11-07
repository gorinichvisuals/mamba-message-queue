namespace MambaMQ.Protocol.Commands.Queues;

public sealed class QueueUpdateCommand(
    Guid queueId,
    string queueName,
    LoadBalancingAlgorithm loadBalancingAlgorithm,
    Dictionary<string, QueuePermission> permissions,
    TimeSpan messageRetentionPeriod) : ICommand
{
    public FrameType Type => FrameType.QueueUpdateCommand;

    public Guid QueueId { get; } = queueId;
    public string QueueName { get; } = queueName;
    public LoadBalancingAlgorithm LoadBalancingAlgorithm { get; } = loadBalancingAlgorithm;
    public Dictionary<string, QueuePermission> Permissions { get; } = permissions;
    public TimeSpan MessageRetentionPeriod { get; } = messageRetentionPeriod;
}