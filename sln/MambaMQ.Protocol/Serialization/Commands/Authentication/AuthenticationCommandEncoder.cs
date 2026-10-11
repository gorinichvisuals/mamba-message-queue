namespace MambaMQ.Protocol.Serialization.Commands.Authentication;

internal static class AuthenticationCommandEncoder
{
    internal static byte[] EncodeAuthentication(AuthenticationCommand command)
    {
        byte[] username = Encoding.UTF8.GetBytes(command.UserName);
        byte[] password = Encoding.UTF8.GetBytes(command.Password);

        byte[] buffer = new byte[
            sizeof(byte) +
            CommandConstants.UsernameLengthSize +
            username.Length +
            CommandConstants.PasswordLengthSize +
            password.Length];

        Span<byte> span = buffer;

        int offset = 0;

        span[offset++] = (byte)command.ClientType;

        offset = ProtocolWriter.WriteString(span, offset, command.UserName);
        ProtocolWriter.WriteString(span, offset, command.Password);

        return buffer;
    }
    
    internal static byte[] EncodeServiceIdentity(ServiceIdentityCommand command)
    {
        byte[] serviceName = Encoding.UTF8.GetBytes(command.ServiceName);

        byte[] buffer = new byte[CommandConstants.ServiceNameLengthSize + serviceName.Length];

        ProtocolWriter.WriteString(buffer, 0, command.ServiceName);

        return buffer;
    }
}