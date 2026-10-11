namespace MambaMQ.Protocol.Serialization.General;

internal static class ProtocolWriter
{
    public static int WriteString(Span<byte> buffer, int offset, string value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value);

        BinaryPrimitives.WriteInt32BigEndian(buffer[offset..], bytes.Length);

        offset += sizeof(int);

        bytes.CopyTo(buffer[offset..]);

        return offset + bytes.Length;
    }
}