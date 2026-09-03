namespace MambaMQ.Protocol.Commands;

public sealed class SubscribeQueueCommand(string queueName, bool autoAcknowledge) : ICommand
{
    public FrameType Type => FrameType.SubscribeQueue;
    public string QueueName { get; } = queueName;
    public bool AutoAcknowledge { get; } = autoAcknowledge;
}