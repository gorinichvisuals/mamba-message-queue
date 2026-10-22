namespace MambaMQ.Protocol.Serialization.Commands.Authentication;

internal static class AuthenticationCommandDecoder
{
    internal static ClientAuthenticateCommand DecodeAuthentication(ReadOnlySpan<byte> buffer)
    {
        if (buffer.IsEmpty)
            throw new InvalidDataException("Authentication command does not contain ClientType.");

        int offset = 0;

        ClientType clientType = (ClientType)buffer[offset++];

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

        return new ClientAuthenticateCommand(username, password, clientType);
    }

    internal static ServiceIdentitySetCommand DecodeServiceIdentity(ReadOnlySpan<byte> buffer)
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