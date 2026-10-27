namespace MambaMQ.Client.NET;

internal sealed partial class MambaClient : IMamba, IAsyncDisposable
{
    private readonly IConnection _connection;
    private readonly MambaClientOptions _options;

    private Task? _connectTask;

    public MambaClient(
        IConnection connection,
        MambaClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(connection);

        _connection = connection;
        _options = options;
    }
    
    public ValueTask DisposeAsync() => _connection.DisposeAsync();
}