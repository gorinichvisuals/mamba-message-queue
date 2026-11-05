namespace MambaMQ.Protocol.Commands.System;

public sealed class AuthenticateCommand(string userName, string password, ClientType clientType) : ICommand
{
    public FrameType Type => FrameType.AuthenticateCommand;
    public string UserName { get; } = userName;
    public string Password { get; } = password;
    public ClientType ClientType  { get; } = clientType;
}