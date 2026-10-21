namespace MambaMQ.Server.Handlers;

internal sealed class PingCommandHandler : ICommandHandler<PingCommand>
{
    public Task Handle(PingCommand command, IClientConnection connection, CancellationToken cancellationToken = default)
        => connection.SendAsync(new Frame(FrameType.PongResponse, ReadOnlyMemory<byte>.Empty), cancellationToken);
}