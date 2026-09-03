namespace MambaMQ.Protocol.Commands;

public sealed class PublishMessageCommand(string queueName, MambaMessage mambaMessage) : ICommand
{
    public FrameType Type => FrameType.PublishMessage;
    public string QueueName { get; } = queueName;
    public MambaMessage MambaMessage { get; } = mambaMessage;
}