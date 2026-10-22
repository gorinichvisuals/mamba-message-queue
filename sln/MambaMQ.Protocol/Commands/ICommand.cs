namespace MambaMQ.Protocol.Commands;

public interface ICommand
{
    FrameType Type { get; }
}