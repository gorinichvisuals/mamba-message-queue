namespace MambaMQ.Protocol.Commands;

public sealed class CreateQueueCommand(
    string queueName, 
    bool isDurable,
    LoadBalancingAlgorithm loadBalancingAlgorithm, 
    bool messageRetentionEnabled,
    TimeSpan messageRetentionPeriod) : ICommand
{
    public FrameType Type => FrameType.CreateQueue;
    public string QueueName { get; } = queueName;
    public bool IsDurable { get; } = isDurable;
    public LoadBalancingAlgorithm LoadBalancingAlgorithm { get; } = loadBalancingAlgorithm;
    public bool MessageRetentionEnabled { get; } =  messageRetentionEnabled;
    public TimeSpan MessageRetentionPeriod { get; } = messageRetentionPeriod;
} 