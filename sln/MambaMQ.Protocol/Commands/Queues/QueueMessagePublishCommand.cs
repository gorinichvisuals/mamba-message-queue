namespace MambaMQ.Protocol.Commands.Queues;

public sealed class QueueMessagePublishCommand(
    string queueName,
    MambaMessage mambaMessage) : ICommand
{
    public FrameType Type => FrameType.QueueMessagePublishCommand;
    public string QueueName { get; } = queueName;
    public MambaMessage MambaMessage { get; } = mambaMessage;
}