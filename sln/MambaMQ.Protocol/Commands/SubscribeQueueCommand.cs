namespace MambaMQ.Protocol.Commands;

public sealed class SubscribeQueueCommand(
    string queueName, 
    bool isDurable, 
    bool persistMessages) : ICommand
{
    public FrameType Type => FrameType.SubscribeQueue;
    public string QueueName { get; } = queueName;
    public bool IsDurable { get; } = isDurable;
    public bool PersistMessages { get; } = persistMessages;
}