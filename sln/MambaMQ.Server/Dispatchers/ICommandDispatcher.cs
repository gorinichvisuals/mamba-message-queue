namespace MambaMQ.Server.Dispatchers;

public interface ICommandDispatcher
{
    Task DispatchAsync(IClientConnection connection, ICommand command, CancellationToken cancellationToken = default);
}