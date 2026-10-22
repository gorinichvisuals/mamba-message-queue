namespace MambaMQ.Protocol.Serialization.Responses.Exchanges;

public static class ExchangeModelDecoder
{
    public static ExchangeModel Decode(ReadOnlySpan<byte> buffer, ref int offset)
    {
        if (buffer.Length - offset < 16)
            throw new InvalidDataException("Exchange response does not contain a complete exchange ID.");

        Guid id = new(buffer.Slice(offset, 16));
        offset += 16;

        string name = ProtocolReader.ReadString(buffer, ref offset, "exchange name", "Get exchanges response");

        if (buffer.Length - offset < sizeof(long))
            throw new InvalidDataException("Get exchanges response does not contain CreatedAt.");

        long createdAtTicks = BinaryPrimitives.ReadInt64BigEndian(buffer[offset..]);
        offset += sizeof(long);
        DateTimeOffset createdAt = new(new DateTime(createdAtTicks, DateTimeKind.Utc));

        if (buffer.Length - offset < 2)
            throw new InvalidDataException("Get exchanges response does not contain exchange flags.");

        byte durableValue = buffer[offset++];
        if (durableValue is not 0 and not 1)
            throw new InvalidDataException("Get exchanges response contains an invalid durability flag.");

        ExchangeType type = (ExchangeType)buffer[offset++];
        if (!Enum.IsDefined(type))
            throw new InvalidDataException("Get exchanges response contains an invalid exchange type.");

        int permissionCount = ReadCount(buffer, ref offset, "permission");
        Dictionary<string, ExchangePermission> permissions = new(permissionCount);

        for (int i = 0; i < permissionCount; i++)
        {
            string serviceName = ProtocolReader.ReadString(buffer, ref offset, "service name", "Get exchanges response");
            if (offset >= buffer.Length)
                throw new InvalidDataException("Get exchanges response does not contain a complete exchange permission.");

            permissions.Add(serviceName, (ExchangePermission)buffer[offset++]);
        }

        int bindingCount = ReadCount(buffer, ref offset, "binding");
        List<ExchangeBindingModel> bindings = new(bindingCount);

        for (int i = 0; i < bindingCount; i++)
        {
            string queueName = ProtocolReader.ReadString(buffer, ref offset, "queue name", "Get exchanges response");
            string routingKey = ProtocolReader.ReadString(buffer, ref offset, "routing key", "Get exchanges response");

            bindings.Add(new ExchangeBindingModel
            {
                QueueName = queueName,
                RoutingKey = routingKey
            });
        }

        return new ExchangeModel
        {
            Id = id,
            Name = name,
            IsDurable = durableValue == 1,
            CreatedAt = createdAt,
            Type = type,
            Permissions = permissions,
            Bindings = bindings
        };
    }

    private static int ReadCount(ReadOnlySpan<byte> buffer, ref int offset, string fieldName)
    {
        if (buffer.Length - offset < sizeof(int))
            throw new InvalidDataException($"Get exchanges response does not contain a {fieldName} count.");

        int count = BinaryPrimitives.ReadInt32BigEndian(buffer[offset..]);
        offset += sizeof(int);

        return count < 0
            ? throw new InvalidDataException($"Get exchanges response contains an invalid {fieldName} count.")
            : count;
    }
}