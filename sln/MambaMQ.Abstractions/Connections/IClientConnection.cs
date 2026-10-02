namespace MambaMQ.Abstractions.Connections;

public interface IClientConnection
{
    Guid Id { get; }
    string ServiceName { get; }
    void Authenticate();
    void IdentifyService(string serviceName);
    Task SendAsync(Frame frame, CancellationToken cancellationToken = default);
}