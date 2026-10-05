namespace MambaMQ.Protocol.Serialization.Responses.Queues;

public static class GetQueuesResponseDecoder
{
    public static GetQueuesResponse Decode(ReadOnlySpan<byte> buffer)
    {
        int offset = 0;

        int queueCount =
            BinaryPrimitives.ReadInt32BigEndian(buffer[offset..]);

        offset += sizeof(int);

        List<QueueModel> queues = new(queueCount);

        for (int i = 0; i < queueCount; i++)
        {
            queues.Add(QueueModelDecoder.Decode(buffer, ref offset));
        }

        return new GetQueuesResponse
        {
            Queues = queues
        };
    }
}