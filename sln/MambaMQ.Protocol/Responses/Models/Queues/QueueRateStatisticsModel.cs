namespace MambaMQ.Protocol.Responses.Models.Queues;

public sealed class QueueRateStatisticsModel
{
    public Guid QueueId { get; init; }
    public long PublishedMessagesPerSecond { get; init; }
    public long DeliveredMessagesPerSecond { get; init; }
}