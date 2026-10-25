namespace MambaMQ.Protocol.Commands.System;

public sealed class ServiceAuthenticateCommand(string serverKey) : ICommand
{
    public FrameType Type => FrameType.ServiceAuthenticateCommand;
    public string ServerKey { get; } = serverKey;
}