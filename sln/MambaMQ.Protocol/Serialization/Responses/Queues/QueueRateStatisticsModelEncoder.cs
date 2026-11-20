namespace MambaMQ.Protocol.Serialization.Responses.Queues;

public static class QueueRateStatisticsModelEncoder
{
    public static byte[] Encode(QueueRateStatisticsModel statistics)
    {
        byte[] buffer = new byte[CommandConstants.QueueIdSize + sizeof(long) * 2];
        Span<byte> span = buffer;
        statistics.QueueId.TryWriteBytes(span);
        BinaryPrimitives.WriteInt64BigEndian(span[CommandConstants.QueueIdSize..], statistics.PublishedMessagesPerSecond);
        BinaryPrimitives.WriteInt64BigEndian(span[(CommandConstants.QueueIdSize + sizeof(long))..], statistics.DeliveredMessagesPerSecond);
        return buffer;
    }
}