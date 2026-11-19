namespace MambaMQ.Protocol.Responses.Models.Queues;

public sealed class QueueStatisticsModel
{
    public Guid QueueId { get; init; }
    public int AvailableMessageCount { get; init; }
    public int InFlightMessageCount { get; init; }
}