namespace MambaMQ.Protocol.Serialization.General;

internal static class ProtocolReader
{
    public static string ReadString(ReadOnlySpan<byte> buffer, ref int offset, string fieldName, string commandName)
    {
        if (buffer.Length - offset < sizeof(int))
            throw new InvalidDataException($"Missing length for '{fieldName}' in {commandName}.");

        int length = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(buffer[offset..]);

        offset += sizeof(int);

        if (length < 0 || length > buffer.Length - offset)
            throw new InvalidDataException($"Invalid length for '{fieldName}' in {commandName}.");

        string value = Encoding.UTF8.GetString(buffer.Slice(offset, length));

        offset += length;

        return value;
    }
}