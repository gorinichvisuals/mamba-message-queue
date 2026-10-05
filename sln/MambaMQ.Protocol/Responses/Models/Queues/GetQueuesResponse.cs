namespace MambaMQ.Protocol.Responses.Models.Queues;

public sealed class GetQueuesResponse
{
    public IReadOnlyList<QueueModel> Queues { get; init; } = [];
}