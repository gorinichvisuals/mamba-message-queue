namespace MambaMQ.Protocol.Serialization.Commands.Exchanges;

internal static class ExchangeCommandDecoder
{
    internal static ExchangeDeleteCommand DecodeDeleteExchange(ReadOnlySpan<byte> buffer)
    {
        return buffer.Length != CommandConstants.ExchangeIdSize
            ? throw new InvalidDataException($"Invalid ExchangeDeleteCommand payload length: {buffer.Length}.")
            : new ExchangeDeleteCommand(new Guid(buffer));
    }

    internal static ExchangeUpdateCommand DecodeUpdateExchange(ReadOnlySpan<byte> buffer)
    {
        int offset = 0;
        
        if (buffer.Length < CommandConstants.ExchangeIdSize)
            throw new InvalidDataException("Update exchange command does not contain ExchangeId.");

        Guid exchangeId = new(buffer[..CommandConstants.ExchangeIdSize]);
        offset += CommandConstants.ExchangeIdSize;
        string exchangeName = ProtocolReader.ReadString(buffer, ref offset, "exchange name", "Update exchange command");
        ValidateIsDurable(buffer, offset);
        bool isDurable = DecodeBoolean(buffer[offset++], "Update exchange command contains invalid IsDurable value.");
        ValidateExchangeType(buffer, offset);
        ExchangeType exchangeType = (ExchangeType)buffer[offset++];
        int permissionsCount = ReadPermissionsCount(buffer, offset);
        offset += CommandConstants.PermissionsCountSize;

        Dictionary<string, ExchangePermission> permissions = [];
        
        for (int i = 0; i < permissionsCount; i++)
        {
            string serviceName = ProtocolReader.ReadString(buffer, ref offset, "service name", "Update exchange command");
            
            if (buffer.Length - offset < CommandConstants.PermissionSize)
                throw new InvalidDataException("Update exchange command does not contain complete permission.");
            
            permissions.Add(serviceName, (ExchangePermission)buffer[offset++]);
        }

        return new ExchangeUpdateCommand(exchangeId, exchangeName, isDurable, exchangeType, permissions);
    }

    internal static ExchangeGetListCommand DecodeGetExchanges()
    {
        return new ExchangeGetListCommand();
    }

    internal static ExchangeCreateCommand DecodeCreateExchange(ReadOnlySpan<byte> buffer)
    {
        int offset = 0;

        string exchangeName = ProtocolReader.ReadString(
            buffer,
            ref offset,
            "exchange name",
            "Create exchange command");

        ValidateIsDurable(buffer, offset);

        bool isDurable = DecodeBoolean(buffer[offset], "Create exchange command contains invalid IsDurable value.");

        offset += CommandConstants.IsDurableSize;

        ValidateExchangeType(buffer, offset);

        ExchangeType exchangeType = (ExchangeType)buffer[offset];
        offset += CommandConstants.ExchangeTypeSize;

        int permissionsCount = ReadPermissionsCount(buffer, offset);
        offset += CommandConstants.PermissionsCountSize;

        Dictionary<string, ExchangePermission> permissions = [];

        for (int i = 0; i < permissionsCount; i++)
        {
            string serviceName = ProtocolReader.ReadString(
                buffer,
                ref offset,
                "service name",
                "Create exchange command");

            if (buffer.Length - offset < CommandConstants.PermissionSize)
                throw new InvalidDataException("Create exchange command does not contain complete permission.");

            ExchangePermission permission = (ExchangePermission)buffer[offset];
            offset += CommandConstants.PermissionSize;

            permissions.Add(serviceName, permission);
        }

        return new ExchangeCreateCommand(
            exchangeName,
            isDurable,
            exchangeType,
            permissions);
    }

    internal static ExchangeBindCommand DecodeBindExchange(ReadOnlySpan<byte> buffer)
    {
        int offset = 0;

        string exchangeName = ProtocolReader.ReadString(
            buffer, ref offset, "exchange name", "Bind exchange command");

        string queueName = ProtocolReader.ReadString(
            buffer, ref offset, "queue name", "Bind exchange command");

        string routingKey = ProtocolReader.ReadString(
            buffer, ref offset, "routing key", "Bind exchange command");

        return new ExchangeBindCommand(exchangeName, queueName, routingKey);
    }

    internal static ExchangeUnbindCommand DecodeUnbindExchange(ReadOnlySpan<byte> buffer)
    {
        int offset = 0;

        string exchangeName = ProtocolReader.ReadString(buffer, ref offset, "exchange name", "Unbind exchange command");

        string queueName = ProtocolReader.ReadString(buffer, ref offset, "queue name", "Unbind exchange command");

        string routingKey = ProtocolReader.ReadString(buffer, ref offset, "routing key", "Unbind exchange command");

        return new ExchangeUnbindCommand(exchangeName, queueName, routingKey);
    }

    internal static ExchangeMessagePublishCommand DecodePublishToExchange(ReadOnlySpan<byte> buffer)
    {
        int offset = 0;

        string exchangeName = ProtocolReader.ReadString(buffer, ref offset, "exchange name", "Publish to exchange command");

        string routingKey = ProtocolReader.ReadString(buffer, ref offset, "routing key", "Publish to exchange command");

        MambaMessage message = MessageDecoder.Decode(buffer[offset..]);

        return new ExchangeMessagePublishCommand(exchangeName, routingKey, message);
    }

    private static int ReadPermissionsCount(ReadOnlySpan<byte> buffer, int offset)
    {
        if (buffer.Length - offset < CommandConstants.PermissionsCountSize)
            throw new InvalidDataException("Create exchange command does not contain permissions count.");

        int count = BinaryPrimitives.ReadInt32BigEndian(buffer.Slice(offset, CommandConstants.PermissionsCountSize));

        return count < 0
            ? throw new InvalidDataException("Create exchange command contains invalid permissions count.")
            : count;
    }

    private static void ValidateIsDurable(ReadOnlySpan<byte> buffer, int offset)
    {
        if (buffer.Length - offset < CommandConstants.IsDurableSize)
            throw new InvalidDataException("Create exchange command does not contain IsDurable.");
    }

    private static bool DecodeBoolean(byte value, string errorMessage)
    {
        if (value is not 0 and not 1)
            throw new InvalidDataException(errorMessage);

        return value == 1;
    }

    private static void ValidateExchangeType(ReadOnlySpan<byte> buffer, int offset)
    {
        if (buffer.Length - offset < CommandConstants.ExchangeTypeSize)
            throw new InvalidDataException("Create exchange command does not contain ExchangeType.");

        byte value = buffer[offset];

        if (!Enum.IsDefined((ExchangeType)value))
            throw new InvalidDataException("Create exchange command contains invalid ExchangeType value.");
    }
}