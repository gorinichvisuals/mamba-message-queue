namespace MambaMQ.Abstractions.Connections;

public interface IClientConnection
{
    Guid Id { get; }
    string ServiceName { get; }
    ClientType ClientType { get; }
    CancellationToken DisconnectedToken { get; }
    void Authenticate(ClientType clientType);
    void IdentifyService(string serviceName);
    Task SendAsync(Frame frame, CancellationToken cancellationToken = default);
}