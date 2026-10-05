namespace MambaMQ.Protocol.Serialization.Responses.Queues;

public static class QueueModelEncoder
{
    public static byte[] Encode(QueueModel queue)
    {
        int size = 16 
                   + sizeof(int) 
                   + Encoding.UTF8.GetByteCount(queue.Name) 
                   + sizeof(long) 
                   + sizeof(byte) 
                   + sizeof(byte) 
                   + sizeof(int) 
                   + queue.Permissions
                        .Sum(permission => sizeof(int) + Encoding.UTF8.GetByteCount(permission.Key) + sizeof(byte));

        size +=
            sizeof(byte) +
            sizeof(long) +
            sizeof(int) +
            sizeof(int);

        byte[] buffer = new byte[size];
        Span<byte> span = buffer;

        int offset = 0;

        queue.Id.TryWriteBytes(span[offset..(offset + 16)]);
        offset += 16;

        offset = WriteString(span, offset, queue.Name);

        BinaryPrimitives.WriteInt64BigEndian(span[offset..], queue.CreatedAt.UtcDateTime.Ticks);

        offset += sizeof(long);

        span[offset++] = queue.IsDurable
            ? (byte)1
            : (byte)0;

        span[offset++] = (byte)queue.LoadBalancingAlgorithm;

        BinaryPrimitives.WriteInt32BigEndian(span[offset..], queue.Permissions.Count);

        offset += sizeof(int);

        foreach (KeyValuePair<string, QueuePermission> permission in queue.Permissions)
        {
            offset = WriteString(span, offset, permission.Key);

            span[offset++] = (byte)permission.Value;
        }

        span[offset++] = queue.MessageRetentionEnabled
            ? (byte)1
            : (byte)0;

        BinaryPrimitives.WriteInt64BigEndian(span[offset..], queue.MessageRetentionPeriod.Ticks);

        offset += sizeof(long);

        BinaryPrimitives.WriteInt32BigEndian(span[offset..], queue.AvailableMessageCount);

        offset += sizeof(int);

        BinaryPrimitives.WriteInt32BigEndian(span[offset..], queue.InFlightMessageCount);

        return buffer;
    }

    private static int WriteString(Span<byte> buffer, int offset, string value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value);

        BinaryPrimitives.WriteInt32BigEndian(buffer[offset..], bytes.Length);

        offset += sizeof(int);

        bytes.CopyTo(buffer[offset..]);

        return offset + bytes.Length;
    }
}