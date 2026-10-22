namespace MambaMQ.Server.Handlers;

public interface ICommandHandler<in TCommand> where TCommand : ICommand
{
    Task Handle(TCommand command, IClientConnection connection, CancellationToken cancellationToken = default);
}