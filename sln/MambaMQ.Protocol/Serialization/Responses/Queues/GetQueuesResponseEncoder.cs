namespace MambaMQ.Protocol.Serialization.Responses.Queues;

public static class GetQueuesResponseEncoder
{
    public static byte[] Encode(GetQueuesResponse response)
    {
        int size = sizeof(int);

        List<byte[]> encodedQueues = [];

        foreach (QueueModel queue in response.Queues)
        {
            byte[] encodedQueue = QueueModelEncoder.Encode(queue);

            encodedQueues.Add(encodedQueue);

            size += encodedQueue.Length;
        }

        byte[] buffer = new byte[size];
        Span<byte> span = buffer;

        int offset = 0;

        BinaryPrimitives.WriteInt32BigEndian(span[offset..], response.Queues.Count);

        offset += sizeof(int);

        foreach (byte[] encodedQueue in encodedQueues)
        {
            encodedQueue.CopyTo(span[offset..]);

            offset += encodedQueue.Length;
        }

        return buffer;
    }
}