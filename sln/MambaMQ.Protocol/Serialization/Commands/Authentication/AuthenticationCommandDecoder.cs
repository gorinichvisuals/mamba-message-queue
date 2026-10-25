namespace MambaMQ.Protocol.Serialization.Commands.Authentication;

internal static class AuthenticationCommandDecoder
{
    internal static ServiceAuthenticateCommand DecodeServiceAuthentication(ReadOnlySpan<byte> buffer)
    {
        int offset = 0;
        string serverKey = ProtocolReader.ReadString(
            buffer,
            ref offset,
            "server key",
            "Authentication command");
        
        return new ServiceAuthenticateCommand(serverKey);
    }

    internal static ManagementAuthenticateCommand DecodeManagementAuthentication(ReadOnlySpan<byte> buffer)
    {
        int offset = 0;
        string username = ProtocolReader.ReadString(buffer, ref offset, "username", "Management authentication command");
        string password = ProtocolReader.ReadString(buffer, ref offset, "password", "Management authentication command");
        
        return new ManagementAuthenticateCommand(username, password);
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