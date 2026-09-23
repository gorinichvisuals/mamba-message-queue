namespace MambaMQ.Protocol.Commands;

public sealed class PublishMessageCommand(
    string queueName, 
    bool isDurable, 
    bool persistMessages,
    MambaMessage mambaMessage) : ICommand
{
    public FrameType Type => FrameType.PublishMessage;
    public string QueueName { get; } = queueName;
    public bool IsDurable { get; } = isDurable;
    public bool PersistMessages { get; } = persistMessages;
    public MambaMessage MambaMessage { get; } = mambaMessage;
}