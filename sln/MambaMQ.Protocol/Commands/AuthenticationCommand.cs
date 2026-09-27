namespace MambaMQ.Protocol.Commands;

public class AuthenticationCommand(string userName, string password) : ICommand
{
    public FrameType Type => FrameType.Authentication;
    public string UserName { get; } = userName;
    public string Password { get; } = password;
}