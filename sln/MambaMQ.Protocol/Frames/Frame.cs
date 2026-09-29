namespace MambaMQ.Protocol.Frames;

public sealed class Frame(FrameType type, ReadOnlyMemory<byte> payload)
{
    public FrameType Type { get; } = type;
    public ReadOnlyMemory<byte> Payload { get; } = payload;
}