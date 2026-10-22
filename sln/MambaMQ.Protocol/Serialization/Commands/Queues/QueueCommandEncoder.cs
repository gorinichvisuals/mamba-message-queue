namespace MambaMQ.Protocol.Serialization.Commands.Queues;

internal static class QueueCommandEncoder
{
    internal static byte[] EncodeCreateQueue(QueueCreateCommand command)
    {
        byte[] queueName = Encoding.UTF8.GetBytes(command.QueueName);

        int permissionsSize = CommandConstants.PermissionsCountSize;

        foreach (KeyValuePair<string, QueuePermission> permission in command.Permissions)
        {
            byte[] serviceName = Encoding.UTF8.GetBytes(permission.Key);

            permissionsSize +=
                CommandConstants.ServiceNameLengthSize +
                serviceName.Length +
                CommandConstants.PermissionSize;
        }

        int offset = CommandConstants.QueueNameLengthSize;

        byte[] buffer = new byte[
            offset +
            queueName.Length +
            CommandConstants.IsDurableSize +
            CommandConstants.LoadBalancingAlgorithmSize +
            permissionsSize +
            CommandConstants.MessageRetentionEnabledSize +
            CommandConstants.MessageRetentionPeriodSize];

        Span<byte> span = buffer;

        offset = ProtocolWriter.WriteString(span, offset - CommandConstants.QueueNameLengthSize, command.QueueName);

        span[offset] = command.IsDurable
            ? (byte)1
            : (byte)0;

        offset += CommandConstants.IsDurableSize;

        span[offset] = (byte)command.LoadBalancingAlgorithm;

        offset += CommandConstants.LoadBalancingAlgorithmSize;

        BinaryPrimitives.WriteInt32BigEndian(span[offset..], command.Permissions.Count);

        offset += CommandConstants.PermissionsCountSize;

        foreach (KeyValuePair<string, QueuePermission> permission in command.Permissions)
        {
            byte[] serviceName = Encoding.UTF8.GetBytes(permission.Key);

            BinaryPrimitives.WriteInt32BigEndian(span[offset..], serviceName.Length);

            offset += CommandConstants.ServiceNameLengthSize;

            serviceName.CopyTo(span[offset..]);

            offset += serviceName.Length;

            span[offset] = (byte)permission.Value;

            offset += CommandConstants.PermissionSize;
        }

        span[offset] = command.MessageRetentionEnabled
            ? (byte)1
            : (byte)0;

        offset += CommandConstants.MessageRetentionEnabledSize;

        BinaryPrimitives.WriteInt64BigEndian(span[offset..], command.MessageRetentionPeriod.Ticks);

        return buffer;
    }

    internal static byte[] EncodeSubscribeQueue(QueueSubscribeCommand command)
    {
        byte[] queueName = Encoding.UTF8.GetBytes(command.QueueName);

        byte[] buffer = new byte[CommandConstants.QueueNameLengthSize + queueName.Length];

        ProtocolWriter.WriteString(buffer, 0, command.QueueName);

        return buffer;
    }

    internal static byte[] EncodeSubscribeQueueWithBatch(QueueSubscribeBatchCommand command)
    {
        byte[] queueName = Encoding.UTF8.GetBytes(command.QueueName);

        int payloadSize =
            CommandConstants.QueueNameLengthSize +
            queueName.Length +
            CommandConstants.MaxMessagesSize +
            CommandConstants.MaxBytesSize +
            CommandConstants.MaxWaitTimeSize +
            CommandConstants.WeightSize;

        byte[] buffer = new byte[payloadSize];

        Span<byte> span = buffer;

        int offset = ProtocolWriter.WriteString(span, 0, command.QueueName);

        BinaryPrimitives.WriteInt32BigEndian(span[offset..], command.MaxMessages);

        offset += CommandConstants.MaxMessagesSize;

        BinaryPrimitives.WriteInt32BigEndian(span[offset..], command.MaxBytes);

        offset += CommandConstants.MaxBytesSize;

        BinaryPrimitives.WriteInt64BigEndian(span[offset..], command.MaxWaitTime.Ticks);

        offset += CommandConstants.MaxWaitTimeSize;

        BinaryPrimitives.WriteInt32BigEndian(span[offset..], command.Weight);

        return buffer;
    }

    internal static byte[] EncodeGetQueues()
    {
        return [];
    }

    internal static byte[] EncodeDeleteQueue(QueueDeleteCommand command)
    {
        byte[] buffer = new byte[16];

        command.QueueId.TryWriteBytes(buffer);

        return buffer;
    }

    internal static byte[] EncodeUpdateQueue(QueueUpdateCommand command)
    {
        byte[] queueName = Encoding.UTF8.GetBytes(command.QueueName);

        int permissionsSize = CommandConstants.PermissionsCountSize + command.Permissions
            .Select(permission => Encoding.UTF8.GetBytes(permission.Key))
            .Select(serviceName => CommandConstants.ServiceNameLengthSize + serviceName.Length + CommandConstants.PermissionSize)
            .Sum();

        int payloadSize =
            CommandConstants.QueueIdSize +
            CommandConstants.QueueNameLengthSize +
            queueName.Length +
            CommandConstants.IsDurableSize +
            CommandConstants.LoadBalancingAlgorithmSize +
            permissionsSize +
            CommandConstants.MessageRetentionEnabledSize +
            CommandConstants.MessageRetentionPeriodSize;

        byte[] buffer = new byte[payloadSize];

        Span<byte> span = buffer;

        int offset = 0;

        command.QueueId.TryWriteBytes(span[offset..]);

        offset += CommandConstants.QueueIdSize;

        offset = ProtocolWriter.WriteString(span, offset, command.QueueName);

        span[offset] = command.IsDurable
            ? (byte)1
            : (byte)0;

        offset += CommandConstants.IsDurableSize;

        span[offset] = (byte)command.LoadBalancingAlgorithm;

        offset += CommandConstants.LoadBalancingAlgorithmSize;

        BinaryPrimitives.WriteInt32BigEndian(span[offset..], command.Permissions.Count);

        offset += CommandConstants.PermissionsCountSize;

        foreach (KeyValuePair<string, QueuePermission> permission in command.Permissions)
        {
            offset = ProtocolWriter.WriteString(span, offset, permission.Key);

            span[offset] = (byte)permission.Value;

            offset += CommandConstants.PermissionSize;
        }

        span[offset] = command.MessageRetentionEnabled
            ? (byte)1
            : (byte)0;

        offset += CommandConstants.MessageRetentionEnabledSize;

        BinaryPrimitives.WriteInt64BigEndian(span[offset..], command.MessageRetentionPeriod.Ticks);

        return buffer;
    }
}