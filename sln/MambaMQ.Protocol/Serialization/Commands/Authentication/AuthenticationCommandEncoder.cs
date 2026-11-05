namespace MambaMQ.Protocol.Serialization.Commands.Authentication;

internal static class AuthenticationCommandEncoder
{
    internal static byte[] EncodeAuthentication(AuthenticateCommand command)
    {
        byte[] username = Encoding.UTF8.GetBytes(command.UserName);
        byte[] password = Encoding.UTF8.GetBytes(command.Password);

        byte[] buffer = new byte[
            CommandConstants.UsernameLengthSize + username.Length +
            CommandConstants.PasswordLengthSize + password.Length +
            CommandConstants.ClientTypeSize];

        Span<byte> span = buffer;

        int offset = ProtocolWriter.WriteString(span, 0, command.UserName);

        offset = ProtocolWriter.WriteString(span, offset, command.Password);

        span[offset] = (byte)command.ClientType;

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