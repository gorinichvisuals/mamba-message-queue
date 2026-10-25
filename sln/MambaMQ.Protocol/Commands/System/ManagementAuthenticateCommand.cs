namespace MambaMQ.Protocol.Commands.System;

public sealed class ManagementAuthenticateCommand(string userName, string password) : ICommand
{
    public FrameType Type => FrameType.ManagementAuthenticateCommand;
    public string UserName { get; } = userName;
    public string Password { get; } = password;
}