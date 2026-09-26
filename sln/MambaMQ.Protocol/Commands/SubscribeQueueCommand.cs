namespace MambaMQ.Protocol.Commands;

public sealed class SubscribeQueueCommand(string queueName) : ICommand
{
    public FrameType Type => FrameType.SubscribeQueue;
    public string QueueName { get; } = queueName;
}