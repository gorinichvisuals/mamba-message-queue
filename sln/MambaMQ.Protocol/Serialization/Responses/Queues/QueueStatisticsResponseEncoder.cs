namespace MambaMQ.Protocol.Serialization.Responses.Queues;

public static class QueueStatisticsResponseEncoder
{
    public static byte[] Encode(IReadOnlyList<QueueStatisticsModel> statistics)
    {
        byte[] buffer = new byte[sizeof(int) + statistics.Count * (CommandConstants.QueueIdSize + sizeof(int) * 2)];
        Span<byte> span = buffer;
        BinaryPrimitives.WriteInt32BigEndian(span, statistics.Count);
        int offset = sizeof(int);

        foreach (QueueStatisticsModel item in statistics)
        {
            item.QueueId.TryWriteBytes(span[offset..]);
            offset += CommandConstants.QueueIdSize;
            BinaryPrimitives.WriteInt32BigEndian(span[offset..], item.AvailableMessageCount);
            offset += sizeof(int);
            BinaryPrimitives.WriteInt32BigEndian(span[offset..], item.InFlightMessageCount);
            offset += sizeof(int);
        }

        return buffer;
    }
}