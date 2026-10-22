namespace MambaMQ.Server.Handlers.System;

internal sealed class ConnectionPingCommandHandler : ICommandHandler<ConnectionPingCommand>
{
    public Task Handle(ConnectionPingCommand command, IClientConnection connection, CancellationToken cancellationToken = default)
        => connection.SendAsync(new Frame(FrameType.ConnectionPongResponse, ReadOnlyMemory<byte>.Empty), cancellationToken);
}