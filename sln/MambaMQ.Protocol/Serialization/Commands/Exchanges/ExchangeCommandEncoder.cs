namespace MambaMQ.Protocol.Serialization.Commands.Exchanges;

internal static class ExchangeCommandEncoder
{
    internal static byte[] EncodeDeleteExchange(ExchangeDeleteCommand command)
    {
        byte[] buffer = new byte[CommandConstants.ExchangeIdSize];
        command.ExchangeId.TryWriteBytes(buffer);
        return buffer;
    }

    internal static byte[] EncodeUpdateExchange(ExchangeUpdateCommand command)
    {
        int permissionsSize = CommandConstants.PermissionsCountSize + command.Permissions
            .Sum(permission => CommandConstants.ServiceNameLengthSize + Encoding.UTF8.GetByteCount(permission.Key) + CommandConstants.PermissionSize);
        byte[] buffer = new byte[
            CommandConstants.ExchangeIdSize +
            CommandConstants.ExchangeNameLengthSize + Encoding.UTF8.GetByteCount(command.ExchangeName) +
            CommandConstants.IsDurableSize +
            CommandConstants.ExchangeTypeSize +
            permissionsSize];

        Span<byte> span = buffer;
        command.ExchangeId.TryWriteBytes(span);
        int offset = CommandConstants.ExchangeIdSize;
        offset = ProtocolWriter.WriteString(span, offset, command.ExchangeName);
        span[offset++] = command.IsDurable ? (byte)1 : (byte)0;
        span[offset++] = (byte)command.ExchangeType;
        BinaryPrimitives.WriteInt32BigEndian(span[offset..], command.Permissions.Count);
        offset += CommandConstants.PermissionsCountSize;

        foreach ((string serviceName, ExchangePermission permission) in command.Permissions)
        {
            offset = ProtocolWriter.WriteString(span, offset, serviceName);
            span[offset++] = (byte)permission;
        }

        return buffer;
    }

    internal static byte[] EncodeGetExchanges() => [];

    internal static byte[] EncodeCreateExchange(ExchangeCreateCommand command)
    {
        byte[] exchangeName = Encoding.UTF8.GetBytes(command.ExchangeName);

        int permissionsSize = CommandConstants.PermissionsCountSize;

        foreach ((string serviceName, ExchangePermission _) in command.Permissions)
        {
            byte[] serviceNameBytes = Encoding.UTF8.GetBytes(serviceName);

            permissionsSize += CommandConstants.ServiceNameLengthSize + serviceNameBytes.Length + CommandConstants.PermissionSize;
        }

        byte[] buffer = new byte[
            CommandConstants.ExchangeNameLengthSize +
            exchangeName.Length +
            CommandConstants.IsDurableSize +
            CommandConstants.ExchangeTypeSize +
            permissionsSize];

        Span<byte> span = buffer;

        int offset = ProtocolWriter.WriteString(span, 0, command.ExchangeName);

        span[offset] = command.IsDurable
            ? (byte)1
            : (byte)0;

        offset += CommandConstants.IsDurableSize;

        span[offset] = (byte)command.ExchangeType;

        offset += CommandConstants.ExchangeTypeSize;

        BinaryPrimitives.WriteInt32BigEndian(span[offset..], command.Permissions.Count);

        offset += CommandConstants.PermissionsCountSize;

        foreach ((string serviceName, ExchangePermission permission) in command.Permissions)
        {
            offset = ProtocolWriter.WriteString(span, offset, serviceName);

            span[offset] = (byte)permission;

            offset += CommandConstants.PermissionSize;
        }

        return buffer;
    }

    internal static byte[] EncodeBindExchange(ExchangeBindCommand command)
    {
        byte[] exchangeName = Encoding.UTF8.GetBytes(command.ExchangeName);
        byte[] queueName = Encoding.UTF8.GetBytes(command.QueueName);
        byte[] routingKey = Encoding.UTF8.GetBytes(command.RoutingKey);

        byte[] buffer = new byte[
            CommandConstants.ExchangeNameLengthSize +
            exchangeName.Length +
            CommandConstants.QueueNameLengthSize +
            queueName.Length +
            CommandConstants.RoutingKeyLengthSize +
            routingKey.Length];

        Span<byte> span = buffer;

        int offset = ProtocolWriter.WriteString(span, 0, command.ExchangeName);

        offset = ProtocolWriter.WriteString(span, offset, command.QueueName);

        ProtocolWriter.WriteString(span, offset, command.RoutingKey);

        return buffer;
    }

    internal static byte[] EncodeUnbindExchange(ExchangeUnbindCommand command)
    {
        byte[] exchangeName = Encoding.UTF8.GetBytes(command.ExchangeName);
        byte[] queueName = Encoding.UTF8.GetBytes(command.QueueName);
        byte[] routingKey = Encoding.UTF8.GetBytes(command.RoutingKey);

        byte[] buffer = new byte[
            CommandConstants.ExchangeNameLengthSize +
            exchangeName.Length +
            CommandConstants.QueueNameLengthSize +
            queueName.Length +
            CommandConstants.RoutingKeyLengthSize +
            routingKey.Length];

        Span<byte> span = buffer;

        int offset = ProtocolWriter.WriteString(span, 0, command.ExchangeName);

        offset = ProtocolWriter.WriteString(span, offset, command.QueueName);

        ProtocolWriter.WriteString(span, offset, command.RoutingKey);

        return buffer;
    }

    internal static byte[] EncodePublishToExchange(ExchangeMessagePublishCommand command)
    {
        byte[] message = MessageEncoder.Encode(command.Message);

        int payloadSize =
            CommandConstants.ExchangeNameLengthSize +
            Encoding.UTF8.GetByteCount(command.ExchangeName) +
            CommandConstants.RoutingKeyLengthSize +
            Encoding.UTF8.GetByteCount(command.RoutingKey) +
            message.Length;

        byte[] buffer = new byte[payloadSize];

        Span<byte> span = buffer;

        int offset = ProtocolWriter.WriteString(span, 0, command.ExchangeName);

        offset = ProtocolWriter.WriteString(span, offset, command.RoutingKey);

        message.CopyTo(span[offset..]);

        return buffer;
    }
}