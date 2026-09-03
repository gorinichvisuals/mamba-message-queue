namespace MambaMQ.Server.Handlers.Abstractions;

public interface ICommandHandler<in TCommand> where TCommand : ICommand
{
    Task HandleAsync(TCommand command, IClientConnection connection, CancellationToken cancellationToken = default);
}