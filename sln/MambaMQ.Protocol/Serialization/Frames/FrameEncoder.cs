namespace MambaMQ.Protocol.Serialization.Frames;

public static class FrameEncoder
{
    public static byte[] Encode(Frame frame)
    {
        int payloadLength = frame.Payload.Length;
        byte[] buffer = new byte[FrameConstants.HeaderSize + payloadLength];
        
        BinaryPrimitives.WriteUInt16BigEndian(
            buffer.AsSpan(
                FrameConstants.MagicOffset,
                FrameConstants.MagicSize), 
            FrameConstants.Magic);
        
        buffer[FrameConstants.VersionOffset] = FrameConstants.Version;
        buffer[FrameConstants.FrameTypeOffset] = (byte)frame.Type;
        
        BinaryPrimitives.WriteInt32BigEndian(
            buffer.AsSpan(
                FrameConstants.PayloadLengthOffset, 
                FrameConstants.PayloadLengthSize), 
            payloadLength);
        
        frame.Payload.Span.CopyTo(buffer.AsSpan(FrameConstants.HeaderSize));
        
        return buffer;
    }
}