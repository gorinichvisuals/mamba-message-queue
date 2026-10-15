namespace MambaMQ.Protocol.Commands;

public sealed class GetMessagesCommand(string queueName, int skip, int take) : ICommand
{
    public FrameType Type => FrameType.GetMessagesCommand;
    public string QueueName { get; } = queueName;
    public int Skip { get; } = skip;
    public int Take { get; } = take;
}