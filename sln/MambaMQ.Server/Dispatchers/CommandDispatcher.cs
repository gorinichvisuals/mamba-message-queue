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
        ICommandHandler<TCommand>? handler = serviceProvider.GetService<ICommandHandler<TCommand>>();

        if (handler is null)
        {
            CommandResponse response = CommandResponse.Fail(
                ErrorCode.UnsupportedCommand,
                $"No handler is registered for command '{command.Type}'.");
            await connection.SendAsync(
                new Frame(FrameType.CommandResultResponse, CommandResponseEncoder.Encode(response)),
                cancellationToken);
            return;
        }

        await handler.Handle(command, connection, cancellationToken);
    }
}