using MambaMQ.Protocol.Enums;

namespace MambaMQ.Abstractions.Connections;

public interface IClientConnection
{
    Guid Id { get; }
    string ServiceName { get; }
    ClientType ClientType { get; }
    void Authenticate(ClientType clientType);
    void IdentifyService(string serviceName);
    Task SendAsync(Frame frame, CancellationToken cancellationToken = default);
}