namespace MambaMQ.Protocol.Commands;

public sealed class DeleteMessagesCommand(string queueName, ICollection<Guid> messageIds) : ICommand
{
    public FrameType Type => FrameType.DeleteSelectedMessagesCommand;
    public string QueueName { get; } = queueName;
    public ICollection<Guid> MessageIds { get; } = messageIds;
}