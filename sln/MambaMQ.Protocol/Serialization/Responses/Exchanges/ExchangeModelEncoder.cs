namespace MambaMQ.Protocol.Serialization.Responses.Exchanges;

public static class ExchangeModelEncoder
{
    public static byte[] Encode(ExchangeModel exchange)
    {
        int size = 16 + sizeof(int) + Encoding.UTF8.GetByteCount(exchange.Name)
                   + sizeof(long) + sizeof(byte) + sizeof(byte) + sizeof(int)
                   + exchange.Permissions.Sum(permission => sizeof(int) + Encoding.UTF8.GetByteCount(permission.Key) + sizeof(byte))
                   + sizeof(int)
                   + exchange.Bindings.Sum(binding =>
                       sizeof(int) + Encoding.UTF8.GetByteCount(binding.QueueName)
                       + sizeof(int) + Encoding.UTF8.GetByteCount(binding.RoutingKey));

        byte[] buffer = new byte[size];
        Span<byte> span = buffer;
        int offset = 0;

        exchange.Id.TryWriteBytes(span[offset..(offset + 16)]);
        offset += 16;

        offset = ProtocolWriter.WriteString(span, offset, exchange.Name);
        BinaryPrimitives.WriteInt64BigEndian(span[offset..], exchange.CreatedAt.UtcDateTime.Ticks);
        offset += sizeof(long);

        span[offset++] = exchange.IsDurable ? (byte)1 : (byte)0;
        span[offset++] = (byte)exchange.Type;

        BinaryPrimitives.WriteInt32BigEndian(span[offset..], exchange.Permissions.Count);
        offset += sizeof(int);

        foreach ((string serviceName, ExchangePermission permission) in exchange.Permissions)
        {
            offset = ProtocolWriter.WriteString(span, offset, serviceName);
            span[offset++] = (byte)permission;
        }

        BinaryPrimitives.WriteInt32BigEndian(span[offset..], exchange.Bindings.Count);
        offset += sizeof(int);

        foreach (ExchangeBindingModel binding in exchange.Bindings)
        {
            offset = ProtocolWriter.WriteString(span, offset, binding.QueueName);
            offset = ProtocolWriter.WriteString(span, offset, binding.RoutingKey);
        }

        return buffer;
    }
}
