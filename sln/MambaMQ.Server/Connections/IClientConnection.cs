namespace MambaMQ.Server.Connections;

public interface IClientConnection
{
    Guid Id { get; }
    Task SendAsync(Frame frame, CancellationToken cancellationToken = default);
}