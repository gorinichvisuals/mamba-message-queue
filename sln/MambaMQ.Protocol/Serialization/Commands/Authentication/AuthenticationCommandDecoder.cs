namespace MambaMQ.Protocol.Serialization.Commands.Authentication;

internal static class AuthenticationCommandDecoder
{
    internal static AuthenticateCommand DecodeAuthentication(
        ReadOnlySpan<byte> buffer)
    {
        int offset = 0;

        string username = ProtocolReader.ReadString(
            buffer,
            ref offset,
            "username",
            "Authentication command");

        string password = ProtocolReader.ReadString(
            buffer,
            ref offset,
            "password",
            "Authentication command");

        if (buffer.Length - offset < CommandConstants.ClientTypeSize)
            throw new InvalidDataException("Missing client type in Authentication command.");

        ClientType clientType = (ClientType)buffer[offset];

        return new AuthenticateCommand(username, password, clientType);
    }

    internal static ServiceIdentitySetCommand DecodeServiceIdentity(
        ReadOnlySpan<byte> buffer)
    {
        int offset = 0;

        string serviceName = ProtocolReader.ReadString(
            buffer,
            ref offset,
            "service name",
            "Service identity command");

        return new ServiceIdentitySetCommand(serviceName);
    }
}