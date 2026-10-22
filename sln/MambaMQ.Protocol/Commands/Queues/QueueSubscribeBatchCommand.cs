namespace MambaMQ.Protocol.Commands.Queues;

public sealed class QueueSubscribeBatchCommand(
    string queueName,
    int maxMessages,
    int maxBytes,
    TimeSpan maxWaitTime,
    int weight) : ICommand
{
    public FrameType Type => FrameType.QueueSubscribeBatchCommand;

    public string QueueName { get; } = queueName;
    public int MaxMessages { get; } = maxMessages;
    public int MaxBytes { get; } = maxBytes;
    public TimeSpan MaxWaitTime { get; } = maxWaitTime;
    public int Weight { get; } = weight;
}