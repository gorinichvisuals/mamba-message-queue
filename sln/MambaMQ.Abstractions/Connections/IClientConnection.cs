namespace MambaMQ.Abstractions.Connections;

public interface IClientConnection
{
    Guid Id { get; }
    void Authenticate();
    Task SendAsync(Frame frame, CancellationToken cancellationToken = default);
}