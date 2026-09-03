namespace MambaMQ.Protocol.Commands;

public sealed class DeleteMessageCommand(string queueName, Guid messageId)  : ICommand
{
    public FrameType Type => FrameType.DeleteMessage;
    public string QueueName { get; } = queueName;
    public Guid MessageId { get; } = messageId;
}