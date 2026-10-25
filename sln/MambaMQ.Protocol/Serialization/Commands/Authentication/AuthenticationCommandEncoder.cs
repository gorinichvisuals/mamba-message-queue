namespace MambaMQ.Protocol.Serialization.Commands.Authentication;

internal static class AuthenticationCommandEncoder
{
    internal static byte[] EncodeServiceAuthentication(ServiceAuthenticateCommand command)
    {
        byte[] serverKey = Encoding.UTF8.GetBytes(command.ServerKey);
        byte[] buffer = new byte[CommandConstants.ServerKeyLengthSize + serverKey.Length];
        
        ProtocolWriter.WriteString(buffer, 0, command.ServerKey);
        
        return buffer;
    }

    internal static byte[] EncodeManagementAuthentication(ManagementAuthenticateCommand command)
    {
        byte[] username = Encoding.UTF8.GetBytes(command.UserName);
        byte[] password = Encoding.UTF8.GetBytes(command.Password);
        byte[] buffer = new byte[
            CommandConstants.UsernameLengthSize + username.Length +
            CommandConstants.PasswordLengthSize + password.Length];

        int offset = ProtocolWriter.WriteString(buffer, 0, command.UserName);
        
        ProtocolWriter.WriteString(buffer, offset, command.Password);
        
        return buffer;
    }

    internal static byte[] EncodeServiceIdentity(ServiceIdentitySetCommand command)
    {
        byte[] serviceName = Encoding.UTF8.GetBytes(command.ServiceName);

        byte[] buffer = new byte[CommandConstants.ServiceNameLengthSize + serviceName.Length];

        ProtocolWriter.WriteString(buffer, 0, command.ServiceName);

        return buffer;
    }
}