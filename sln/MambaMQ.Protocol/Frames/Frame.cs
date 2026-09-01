namespace MambaMQ.Protocol.Frames;

public sealed class Frame(FrameType type, ReadOnlyMemory<byte> payload)
{
    public FrameType Type { get; }  = type;
    public ReadOnlyMemory<byte> Payload { get; }  = payload;
}

public enum FrameType : byte
{
    Publish = 1,
    Subscribe = 2,
    Delete = 3
}