namespace MambaMQ.Protocol.Commands;

public sealed class ServiceIdentityCommand(string serviceName) : ICommand
{
    public FrameType Type => FrameType.ServiceIdentity;

    public string ServiceName { get; } = serviceName;
}