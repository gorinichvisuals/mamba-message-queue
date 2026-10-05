namespace MambaMQ.Protocol.Responses.Models.Queues;

public sealed class CreateQueueResponse
{
    public required QueueModel Queue { get; init; }
}