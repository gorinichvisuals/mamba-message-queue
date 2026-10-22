namespace MambaMQ.Protocol.Commands.System;

public sealed class ServiceIdentitySetCommand(string serviceName) : ICommand
{
    public FrameType Type => FrameType.ServiceIdentitySetCommand;

    public string ServiceName { get; } = serviceName;
}