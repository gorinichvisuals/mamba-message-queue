namespace MambaMQ.Client.Readers;

internal static class FrameReader
{
    public static async Task<Frame> ReadAsync(IConnection connection, int maxMessageSizeInKilobytes, CancellationToken cancellationToken = default)
    {
        byte[] header = new byte[FrameConstants.HeaderSize];

        await ReadExactlyAsync(connection, header, cancellationToken);

        int payloadLength = BinaryPrimitives.ReadInt32BigEndian(
            header.AsSpan(
                FrameConstants.PayloadLengthOffset,
                FrameConstants.PayloadLengthSize));

        if (payloadLength < 0)
            throw new InvalidDataException("Invalid payload length.");

        if (payloadLength > maxMessageSizeInKilobytes)
            throw new InvalidDataException($"Payload is too large. Maximum size is {maxMessageSizeInKilobytes} bytes.");

        byte[] buffer = new byte[FrameConstants.HeaderSize + payloadLength];

        header.CopyTo(buffer, 0);

        if (payloadLength > 0)
            await ReadExactlyAsync(connection, buffer.AsMemory(FrameConstants.HeaderSize), cancellationToken);

        return FrameDecoder.Decode(buffer);
    }

    private static async Task ReadExactlyAsync(IConnection connection, Memory<byte> buffer, CancellationToken cancellationToken)
    {
        while (!buffer.IsEmpty)
        {
            int bytesRead = await connection.ReceiveAsync(buffer, cancellationToken);

            if (bytesRead is 0)
                throw new IOException("Server disconnected.");

            buffer = buffer[bytesRead..];
        }
    }
}