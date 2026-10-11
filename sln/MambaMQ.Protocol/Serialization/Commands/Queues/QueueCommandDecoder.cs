namespace MambaMQ.Protocol.Serialization.Commands.Queues;

internal static class QueueCommandDecoder
{
    internal static CreateQueueCommand DecodeCreateQueue(ReadOnlySpan<byte> buffer)
    {
        int offset = 0;

        string queueName = ProtocolReader.ReadString(
            buffer, ref offset, "queue name", "Create queue command");

        bool isDurable = ReadBoolean(buffer, ref offset, "IsDurable", "Create queue command");

        LoadBalancingAlgorithm algorithm = ReadLoadBalancingAlgorithm(buffer, ref offset);

        Dictionary<string, QueuePermission> permissions = ReadQueuePermissions(buffer, ref offset, "Create queue command");

        bool retentionEnabled = ReadBoolean(buffer, ref offset, "MessageRetentionEnabled", "Create queue command");

        TimeSpan retentionPeriod = ReadTimeSpan(
            buffer, ref offset,
            CommandConstants.MessageRetentionPeriodSize,
            "MessageRetentionPeriod",
            "Create queue command");

        return new CreateQueueCommand(
            queueName,
            isDurable,
            algorithm,
            permissions,
            retentionEnabled,
            retentionPeriod);
    }

    internal static SubscribeQueueCommand DecodeSubscribeQueue(ReadOnlySpan<byte> buffer)
    {
        int offset = 0;

        string queueName = ProtocolReader.ReadString(buffer, ref offset, "queue name", "Subscribe queue command");

        return new SubscribeQueueCommand(queueName);
    }

    internal static SubscribeQueueWithBatchCommand DecodeSubscribeQueueWithBatch(ReadOnlySpan<byte> buffer)
    {
        int offset = 0;

        string queueName = ProtocolReader.ReadString(buffer, ref offset, "queue name", "Subscribe with batch command");

        int maxMessages = ReadInt32(buffer, ref offset, CommandConstants.MaxMessagesSize, "MaxMessages", "Subscribe with batch command");

        int maxBytes = ReadInt32(buffer, ref offset, CommandConstants.MaxBytesSize, "MaxBytes", "Subscribe with batch command");

        TimeSpan maxWaitTime = ReadTimeSpan(buffer, ref offset, CommandConstants.MaxWaitTimeSize, "MaxWaitTime", "Subscribe with batch command");

        int weight = ReadInt32(buffer, ref offset, CommandConstants.WeightSize, "Weight", "Subscribe with batch command");

        return new SubscribeQueueWithBatchCommand(queueName, maxMessages, maxBytes, maxWaitTime, weight);
    }

    internal static GetQueuesCommand DecodeGetQueues()
    {
        return new GetQueuesCommand();
    }

    internal static DeleteQueueCommand DecodeDeleteQueue(ReadOnlySpan<byte> buffer)
    {
        return buffer.Length != CommandConstants.QueueIdSize 
            ? throw new InvalidDataException($"Invalid DeleteQueueCommand payload length: {buffer.Length}.") 
            : new DeleteQueueCommand(new Guid(buffer));
    }

    internal static UpdateQueueCommand DecodeUpdateQueue(ReadOnlySpan<byte> buffer)
    {
        int offset = 0;

        if (buffer.Length - offset < CommandConstants.QueueIdSize)
            throw new InvalidDataException("Update queue command does not contain QueueId.");

        Guid queueId = new(buffer.Slice(offset, CommandConstants.QueueIdSize));

        offset += CommandConstants.QueueIdSize;

        string queueName = ProtocolReader.ReadString(buffer, ref offset, "queue name", "Update queue command");

        bool isDurable = ReadBoolean(buffer, ref offset, "IsDurable", "Update queue command");

        LoadBalancingAlgorithm algorithm = ReadLoadBalancingAlgorithm(buffer, ref offset);

        Dictionary<string, QueuePermission> permissions = ReadQueuePermissions(buffer, ref offset, "Update queue command");

        bool retentionEnabled = ReadBoolean(buffer, ref offset, "MessageRetentionEnabled", "Update queue command");

        TimeSpan retentionPeriod = ReadTimeSpan(
            buffer, ref offset,
            CommandConstants.MessageRetentionPeriodSize,
            "MessageRetentionPeriod",
            "Update queue command");

        return new UpdateQueueCommand(
            queueId,
            queueName,
            isDurable,
            algorithm,
            permissions,
            retentionEnabled,
            retentionPeriod);
    }

    private static Dictionary<string, QueuePermission> ReadQueuePermissions(ReadOnlySpan<byte> buffer, ref int offset, string commandName)
    {
        int count = ReadInt32(buffer, ref offset, CommandConstants.PermissionsCountSize, "permissions count", commandName);

        if (count < 0)
            throw new InvalidDataException($"{commandName} contains invalid permissions count.");

        Dictionary<string, QueuePermission> permissions = [];

        for (int i = 0; i < count; i++)
        {
            string serviceName = ProtocolReader.ReadString(buffer, ref offset, "service name", commandName);

            if (buffer.Length - offset < CommandConstants.PermissionSize)
                throw new InvalidDataException($"{commandName} does not contain complete permission.");

            QueuePermission permission = (QueuePermission)buffer[offset];
            offset += CommandConstants.PermissionSize;

            permissions.Add(serviceName, permission);
        }

        return permissions;
    }

    private static bool ReadBoolean(
        ReadOnlySpan<byte> buffer,
        ref int offset,
        string fieldName,
        string commandName)
    {
        if (buffer.Length - offset < sizeof(byte))
            throw new InvalidDataException($"{commandName} does not contain {fieldName}.");

        byte value = buffer[offset++];

        if (value is not 0 and not 1)
            throw new InvalidDataException($"{commandName} contains invalid {fieldName} value.");

        return value == 1;
    }

    private static LoadBalancingAlgorithm ReadLoadBalancingAlgorithm(ReadOnlySpan<byte> buffer, ref int offset)
    {
        if (buffer.Length - offset < CommandConstants.LoadBalancingAlgorithmSize)
            throw new InvalidDataException("Queue command does not contain LoadBalancingAlgorithm.");

        byte value = buffer[offset++];

        if (!Enum.IsDefined((LoadBalancingAlgorithm)value))
            throw new InvalidDataException("Queue command contains invalid LoadBalancingAlgorithm value.");

        return (LoadBalancingAlgorithm)value;
    }

    private static int ReadInt32(
        ReadOnlySpan<byte> buffer,
        ref int offset,
        int size,
        string fieldName,
        string commandName)
    {
        if (buffer.Length - offset < size)
            throw new InvalidDataException($"{commandName} does not contain {fieldName}.");

        int value = BinaryPrimitives.ReadInt32BigEndian(buffer.Slice(offset, size));

        offset += size;

        return value;
    }

    private static TimeSpan ReadTimeSpan(
        ReadOnlySpan<byte> buffer,
        ref int offset,
        int size,
        string fieldName,
        string commandName)
    {
        if (buffer.Length - offset < size)
            throw new InvalidDataException($"{commandName} does not contain {fieldName}.");

        long ticks = BinaryPrimitives.ReadInt64BigEndian(buffer.Slice(offset, size));

        offset += size;

        return TimeSpan.FromTicks(ticks);
    }
}