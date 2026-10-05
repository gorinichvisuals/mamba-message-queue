namespace MambaMQ.Protocol.Commands;

public class AuthenticationCommand(
    string userName, 
    string password, 
    ClientType clientType) : ICommand
{
    public FrameType Type => FrameType.Authentication;
    public string UserName { get; } = userName;
    public string Password { get; } = password;
    public ClientType ClientType { get; } = clientType;
}