namespace MambaMQ.Protocol.Commands.Abstractions;

public interface ICommand
{
    FrameType Type { get; }
}