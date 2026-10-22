namespace MambaMQ.Protocol.Commands.Queues;

public sealed class QueueMessagesGetCommand(string queueName, int skip, int take) : ICommand
{
    public FrameType Type => FrameType.QueueMessagesGetCommand;
    public string QueueName { get; } = queueName;
    public int Skip { get; } = skip;
    public int Take { get; } = take;
}