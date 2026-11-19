namespace MambaMQ.Protocol.Serialization.Responses.Queues;

public static class QueueStatisticsResponseDecoder
{
    public static IReadOnlyList<QueueStatisticsModel> Decode(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length < sizeof(int))
            throw new InvalidDataException("Queue statistics response does not contain a count.");

        int count = BinaryPrimitives.ReadInt32BigEndian(buffer);
        int itemSize = CommandConstants.QueueIdSize + sizeof(int) * 2;
        if (count < 0 || (long)buffer.Length != sizeof(int) + (long)count * itemSize)
            throw new InvalidDataException("Queue statistics response has an invalid length.");

        List<QueueStatisticsModel> result = new(count);
        int offset = sizeof(int);
        for (int i = 0; i < count; i++)
        {
            Guid id = new(buffer.Slice(offset, CommandConstants.QueueIdSize));
            offset += CommandConstants.QueueIdSize;
            int available = BinaryPrimitives.ReadInt32BigEndian(buffer[offset..]);
            offset += sizeof(int);
            int inFlight = BinaryPrimitives.ReadInt32BigEndian(buffer[offset..]);
            offset += sizeof(int);
            result.Add(new QueueStatisticsModel { QueueId = id, AvailableMessageCount = available, InFlightMessageCount = inFlight });
        }

        return result;
    }
}