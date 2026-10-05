namespace MambaMQ.Protocol.Serialization.Responses.Queues;

public static class QueueModelDecoder
{
    public static QueueModel Decode(ReadOnlySpan<byte> buffer)
    {
        int offset = 0;

        return Decode(buffer, ref offset);
    }
    
    public static QueueModel Decode(ReadOnlySpan<byte> buffer, ref int offset)
    {
        Guid id = new(buffer[offset..(offset + 16)]);
        offset += 16;

        string name = DecodeString(buffer, ref offset);

        long createdAtTicks = BinaryPrimitives.ReadInt64BigEndian(buffer[offset..]);

        offset += sizeof(long);

        bool isDurable = buffer[offset++] != 0;

        LoadBalancingAlgorithm loadBalancingAlgorithm = (LoadBalancingAlgorithm)buffer[offset++];

        int permissionCount = BinaryPrimitives.ReadInt32BigEndian(buffer[offset..]);

        offset += sizeof(int);

        Dictionary<string, QueuePermission> permissions = new(permissionCount);

        for (int i = 0; i < permissionCount; i++)
        {
            string serviceName = DecodeString(buffer, ref offset);

            QueuePermission permission = (QueuePermission)buffer[offset++];

            permissions.Add(serviceName, permission);
        }

        bool messageRetentionEnabled = buffer[offset++] != 0;

        long messageRetentionTicks = BinaryPrimitives.ReadInt64BigEndian(buffer[offset..]);

        offset += sizeof(long);

        int availableMessageCount = BinaryPrimitives.ReadInt32BigEndian(buffer[offset..]);

        offset += sizeof(int);

        int inFlightMessageCount = BinaryPrimitives.ReadInt32BigEndian(buffer[offset..]);

        offset += sizeof(int);

        return new QueueModel
        {
            Id = id,
            Name = name,
            CreatedAt = new DateTimeOffset(new DateTime(createdAtTicks, DateTimeKind.Utc)),
            IsDurable = isDurable,
            LoadBalancingAlgorithm = loadBalancingAlgorithm,
            Permissions = permissions,
            MessageRetentionEnabled = messageRetentionEnabled,
            MessageRetentionPeriod = TimeSpan.FromTicks(messageRetentionTicks),
            AvailableMessageCount = availableMessageCount,
            InFlightMessageCount = inFlightMessageCount
        };
    }

    private static string DecodeString(ReadOnlySpan<byte> buffer, ref int offset)
    {
        int length = BinaryPrimitives.ReadInt32BigEndian(buffer[offset..]);

        offset += sizeof(int);

        string value = Encoding.UTF8.GetString(buffer.Slice(offset, length));

        offset += length;

        return value;
    }
}