namespace MambaMQ.Protocol.Commands.System;

public class ClientAuthenticateCommand(
    string userName,
    string password,
    ClientType clientType) : ICommand
{
    public FrameType Type => FrameType.ClientAuthenticateCommand;
    public string UserName { get; } = userName;
    public string Password { get; } = password;
    public ClientType ClientType { get; } = clientType;
}