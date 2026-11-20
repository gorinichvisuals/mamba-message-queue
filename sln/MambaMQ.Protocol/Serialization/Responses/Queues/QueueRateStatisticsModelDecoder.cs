namespace MambaMQ.Protocol.Serialization.Responses.Queues;

public static class QueueRateStatisticsModelDecoder
{
    public static QueueRateStatisticsModel Decode(ReadOnlySpan<byte> buffer)
    {
        int expectedLength = CommandConstants.QueueIdSize + sizeof(long) * 2;
        if (buffer.Length != expectedLength)
            throw new InvalidDataException($"Invalid queue rate statistics payload length: {buffer.Length}.");

        return new QueueRateStatisticsModel
        {
            QueueId = new Guid(buffer[..CommandConstants.QueueIdSize]),
            PublishedMessagesPerSecond = BinaryPrimitives.ReadInt64BigEndian(buffer[CommandConstants.QueueIdSize..]),
            DeliveredMessagesPerSecond = BinaryPrimitives.ReadInt64BigEndian(buffer[(CommandConstants.QueueIdSize + sizeof(long))..])
        };
    }
}