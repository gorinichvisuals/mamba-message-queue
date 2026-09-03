namespace MambaMQ.Server.Dispatchers;

internal sealed class CommandDispatcher(IServiceProvider serviceProvider) : ICommandDispatcher
{
    public Task DispatchAsync(IClientConnection connection, ICommand command, CancellationToken cancellationToken = default)
    {
        Type commandType = command.GetType();

        MethodInfo method = typeof(CommandDispatcher)
            .GetMethod(
                nameof(DispatchInternalAsync),
                BindingFlags.Instance | BindingFlags.NonPublic)!
            .MakeGenericMethod(commandType);

        return (Task)method.Invoke(this, [connection, command, cancellationToken])!;
    }

    private async Task DispatchInternalAsync<TCommand>(IClientConnection connection, TCommand command, CancellationToken cancellationToken)
        where TCommand : ICommand
    {
        ICommandHandler<TCommand> handler =
            serviceProvider.GetRequiredService<ICommandHandler<TCommand>>();

        await handler.HandleAsync(command, connection, cancellationToken);
    }
}