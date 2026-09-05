namespace MambaMQ.Protocol.Serialization.Messages;

public static class MessageDecoder
{
    public static MambaMessage Decode(ReadOnlySpan<byte> buffer)
    {
        Validate(buffer);

        Guid messageId = DecodeMessageId(buffer);
        DateTimeOffset receivedAt = DecodeReceivedAt(buffer);
        int bodyLength = DecodeBodyLength(buffer);
        ReadOnlyMemory<byte> body = DecodePayload(buffer, bodyLength);

        return new MambaMessage(messageId, receivedAt, body);
    }

    private static void Validate(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length < MessageConstants.HeaderSize)
            throw new ArgumentException(
                "Message data is too short.",
                nameof(buffer));
    }

    private static Guid DecodeMessageId(ReadOnlySpan<byte> buffer)
        => new(buffer[..MessageConstants.MessageIdSize]);

    private static DateTimeOffset DecodeReceivedAt(ReadOnlySpan<byte> buffer)
    {
        long utcTicks = BinaryPrimitives.ReadInt64BigEndian(
            buffer.Slice(
                MessageConstants.MessageIdSize,
                MessageConstants.ReceivedAtSize));

        return new DateTimeOffset(
            new DateTime(utcTicks, DateTimeKind.Utc));
    }

    private static int DecodeBodyLength(ReadOnlySpan<byte> buffer)
    {
        int bodyLength = BinaryPrimitives.ReadInt32BigEndian(
            buffer.Slice(
                MessageConstants.BodyLengthOffset,
                MessageConstants.BodyLengthSize));

        if (bodyLength < 0)
            throw new ArgumentException(
                "Invalid payload length.",
                nameof(buffer));

        return buffer.Length != MessageConstants.HeaderSize + bodyLength
            ? throw new ArgumentException(
                "Message data has an invalid payload length.",
                nameof(buffer))
            : bodyLength;
    }

    private static ReadOnlyMemory<byte> DecodePayload(
        ReadOnlySpan<byte> buffer,
        int bodyLength)
        => buffer
            .Slice(MessageConstants.HeaderSize, bodyLength)
            .ToArray();
}